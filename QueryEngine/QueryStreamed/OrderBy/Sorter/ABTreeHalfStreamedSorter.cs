using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a half streamed order by.
    /// Each matcher/thread orders it is found results locally using AB tree.
    /// When the search is done, the results are merged using parallel Merge
    /// from the HPCsharp library. This is done in two steps, firstly the 
    /// ordered sequences from the ab trees are copied into an array which is
    /// passed into the library functions. The merge function it self merges only two 
    /// sub arrays of the array. Thus the merging tree has the height O(log(n)) where 
    /// n is equal to the number of working threads/matchers.
    /// </summary>
    internal class ABTreeHalfStreamedSorter : OrderByResultProcessor
    {
        /// <summary>
        /// Each job has a local tree and a local result table.
        /// </summary>
        private SortJob[] sortJobs;
        private int sortJobsFinished = 0;
        /// <summary>
        /// If it runs in parallel, the class will merge non empty results of the sortJobs.
        /// </summary>
        private MergeObject mergeJob;
        /// <summary>
        /// If it runs in parallel, the final results will be stored inside.
        /// </summary>
        private TableResults.RowProxy[] mergedResults;

        public ABTreeHalfStreamedSorter(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount) 
            : base(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount) 
        {
            var tmpComp = new RowComparer(this.comparers);
            this.sortJobs = new SortJob[this.executionHelper.ThreadCount];
            for (int i = 0; i < sortJobs.Length; i++)
            {
                var results = new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize);
                this.sortJobs[i] = new SortJob(this.CreateComparer(tmpComp, results), results);
            }
        } 

        /// <summary>
        /// Inserts result into a table and also to the ab tree.
        /// When the matcher is finished, then if it run in parallel, results of each matcher
        /// are merged into the mergedResults variable.
        /// Note that even if it runs in parallel, and there are no resuls or just one matcher
        /// found something, the results will be preserved and the method RetrieveResults will behave
        /// as if it run in single-threaded.
        /// </summary>
        public override void Process(int matcherID, Element[] result)
        {
            var job = this.sortJobs[matcherID];
            if (result != null)
            {
                job.results.StoreRow(result);
                job.tree.Insert(job.results.RowCount - 1);
            } else
            {
                if (this.sortJobs.Length > 1)
                {
                    // Last finished thread, inits merging of the results.
                    if (Interlocked.Increment(ref this.sortJobsFinished) == this.executionHelper.ThreadCount)
                    {
                        this.mergeJob = new MergeObject(this.sortJobs, new RowComparer(this.comparers));
                        if (this.mergeJob.jobsToMerge.Length >= 2)
                        {
                            this.sortJobs = null;
                            this.MergeResuls();
                        }
                        else if (this.mergeJob.jobsToMerge.Length == 1) this.sortJobs = this.mergeJob.jobsToMerge;
                        else this.sortJobs = new SortJob[] { this.sortJobs[0] }; 
                    }
                }
            }
        }

        private void MergeResuls() 
        {
            this.mergedResults = this.mergeJob.Merge();
        } 

        /// <summary>
        /// Class represents information about merging.
        /// </summary>
        private class MergeObject
        {
            /// <summary>
            /// SortJobs that have found at least one element during matching.
            /// </summary>
            public SortJob[] jobsToMerge;
            /// <summary>
            /// Cache must be off.
            /// </summary>
            public RowComparer comparer;
            public TableResults.RowProxy[] source;
            public TableResults.RowProxy[] destination;
            public int[] startRanges;

            /// <summary>
            /// Chooses only jobs that had non zero results during search.
            /// </summary>
            public MergeObject(SortJob[] jobs, RowComparer comparer)
            {
                List<SortJob> mergeJobs = new List<SortJob>();
                List<int> startRan = new List<int>();
                
                this.comparer = comparer;
                this.comparer.SetCaching(false);

                int count = 0;
                for (int i = 0; i < jobs.Length; i++)
                {
                    if (jobs[i].tree.Count == 0) continue;
                    else
                    {
                        startRan.Add(count);
                        mergeJobs.Add(jobs[i]);
                        count += jobs[i].tree.Count;
                    }
                }

                this.source = new TableResults.RowProxy[count];
                this.destination = new TableResults.RowProxy[count];
                this.startRanges = startRan.ToArray();
                this.jobsToMerge = mergeJobs.ToArray();
            }

            public int GetStartOfRange(int jobIndex)
            {
                return this.startRanges[jobIndex];
            }
            public int GetRange(int jobIndex)
            {
                if (jobIndex + 1 == this.jobsToMerge.Length)
                    return this.jobsToMerge.Length - this.GetStartOfRange(jobIndex);
                else return this.GetStartOfRange(jobIndex + 1) - this.GetStartOfRange(jobIndex);
            }

            /// <summary>
            /// Loses reference to the tree, so that it will not take memory during merging.
            /// </summary>
            private void ClearTree(int jobIndex)
            {
                this.jobsToMerge[jobIndex].tree = null;
            }

            /// <summary>
            /// Copy results of one sortJob into array that will be provided for the Merge methods. 
            /// After copying the tree can be removed.
            /// </summary>
            /// <param name="jobIndex"> Results to copy. </param>
            /// <param name="startIndex"> An index where to start inserting elements to the source array. </param>
            /// <returns> The number of elements copied. </returns>
            private int CopySortJobResultsAndClearTree(int jobIndex, TableResults.RowProxy[] arr)
            {
                var job = this.jobsToMerge[jobIndex];

                int i = this.startRanges[jobIndex];
                foreach (var item in job.tree)
                {
                    arr[i] = job.results[item];
                    i++;
                }
                int treeCount = job.tree.Count;
                this.ClearTree(jobIndex);
                return treeCount;
            }
            

            public TableResults.RowProxy[] Merge()
            {
                var rootLevel = this.MergeResultsParallel(0, this.jobsToMerge.Length);
                if (rootLevel.Item1) return this.source;
                else return this.destination;
            }

            /// <summary>
            /// The method is initially called only if the end - start >= 2.
            /// </summary>
            /// <returns> 
            /// True if the upper layer should merge from source to destination otherwise false.
            /// The integer stands for the length of the merged sequence from the lower leyer.
            /// </returns>
            public Tuple<bool,int> MergeResultsParallel(int start, int end)
            {
                // Internal node
                if (end - start > 3)
                {
                    // compute middle of the range
                    int middle = ((end - start) / 2) + start;
                    if (middle % 2 == 1) middle--;

                    Task<Tuple<bool, int>> task = Task<Tuple<bool, int>>.Factory.StartNew(() => MergeResultsParallel(middle, end));
                    // Current thread work.
                    var leftRes = MergeResultsParallel(start, middle);
                    var rightRes = task.Result;
                    // Merge
                
                    // Source to destination
                    if (leftRes.Item1)
                    {
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                            this.source,                // Merge from 
                            this.GetStartOfRange(start),  // Start of the left part
                            leftRes.Item2,              // Right part length
                            this.GetStartOfRange(middle), // Start of the right part
                            rightRes.Item2,             // Right part length
                            this.destination,           // Merge to
                            this.GetStartOfRange(start),  // Index of the merged result
                            this.comparer);             // Comparer
                        return new Tuple<bool, int>(false, leftRes.Item2 + rightRes.Item2);
                    }
                    // Destination to source
                    else
                    {
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                            this.destination,           // Merge from 
                            this.GetStartOfRange(start),  // Start of the left part
                            leftRes.Item2,              // Left part length
                            this.GetStartOfRange(middle), // Start of the Reft part
                            rightRes.Item2,             // Reft part length
                            this.source,                // Merge to
                            this.GetStartOfRange(start),  // Index of the merged result
                            this.comparer);             // Comparer
                        return new Tuple<bool, int>(true, leftRes.Item2 + rightRes.Item2);
                    }
                }
                else
                {   // The length of the range is always either 2 or 3
                    if (end - start == 2)
                    {
                        Parallel.Invoke(() => CopySortJobResultsAndClearTree(start, this.source),
                                        () => CopySortJobResultsAndClearTree(start+1, this.source));
                        
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                           this.source,                // Merge from 
                           this.GetStartOfRange(start),  // Start of the left part
                           this.GetRange(start),              // Right part length
                           this.GetStartOfRange(start + 1), // Start of the right part
                           this.GetRange(start + 1),             // Right part length
                           this.destination,           // Merge to
                           this.GetStartOfRange(start),  // Index of the merged result
                           this.comparer);             // Comparer
                        return new Tuple<bool, int>(false, this.GetRange(start) + this.GetRange(start + 1));
                    } else
                    {
                        Parallel.Invoke(() => CopySortJobResultsAndClearTree(start, this.destination),
                                        () => CopySortJobResultsAndClearTree(start + 1, this.destination),
                                        () => CopySortJobResultsAndClearTree(start + 2, this.source));

                        // First merge  2 from destination to source.
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                          this.destination,                      // Merge from 
                          this.GetStartOfRange(start),           // Start of the left part
                          this.GetRange(start),                  // Right part length
                          this.GetStartOfRange(start + 1),       // Start of the right part
                          this.GetRange(start + 1),              // Right part length
                          this.source,                           // Merge to
                          this.GetStartOfRange(start),           // Index of the merged result
                          this.comparer);                        // Comparer
                        // Then merge the result of the above 2 with the third one into the destination.
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                          this.source,                                      // Merge from 
                          this.GetStartOfRange(start),                      // Start of the left part
                          this.GetRange(start) + this.GetRange(start + 1),  // Right part length
                          this.GetStartOfRange(start + 2),                  // Start of the right part
                          this.GetRange(start + 2),                         // Right part length
                          this.destination,                                 // Merge to
                          this.GetStartOfRange(start),                      // Index of the merged result
                          this.comparer);                                   // Comparer
                        return new Tuple<bool, int>(false, this.GetRange(start) + this.GetRange(start + 1) + this.GetRange(start + 2));
                    }
                }
            }
        }

        private class SortJob
        {
            public ABTree<int> tree;
            public TableResults results;

            public SortJob(IComparer<int> comparer, TableResults results)
            {
                this.tree = new ABTree<int>(256, comparer);
                this.results = results;
            }
        }

        private IndexToRowProxyComparerNoDup CreateComparer(RowComparer comparer, TableResults results) 
        {
            var newComparer = comparer.Clone();
            newComparer.SetCaching(true);
            return new IndexToRowProxyComparerNoDup(newComparer, results);
        }
        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            throw new NotImplementedException();
        }
    }
}
