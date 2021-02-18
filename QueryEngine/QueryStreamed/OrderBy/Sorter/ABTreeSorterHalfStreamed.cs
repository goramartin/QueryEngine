using System;
using System.Collections.Generic;
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
            this.sortJobs = new SortJob[this.executionHelper.ThreadCount];
            for (int i = 0; i < sortJobs.Length; i++)
            {
                var results = new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize);
                this.sortJobs[i] = new SortJob(new IndexToRowProxyComparer(RowComparer.Factory(this.comparers, true), results, false), results);
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
                job.resTable.StoreRow(result);
                job.tree.Insert(job.resTable.RowCount - 1);
            } else
            {
                if (this.sortJobs.Length > 1)
                {
                    // Last finished thread, inits merging of the results.
                    if (Interlocked.Increment(ref this.sortJobsFinished) == this.executionHelper.ThreadCount)
                    {
                        this.mergeJob = new MergeObject(this.sortJobs, RowComparer.Factory(this.comparers, false));
                        if (this.mergeJob.jobsToMerge.Length >= 2)
                        {
                            this.sortJobs = null;
                            this.mergedResults = this.MergeResuls();
                        }
                        else if (this.mergeJob.jobsToMerge.Length == 1) this.sortJobs = this.mergeJob.jobsToMerge;
                        else this.sortJobs = new SortJob[] { this.sortJobs[0] }; 
                    }
                }
            }
        }

        private TableResults.RowProxy[] MergeResuls() 
        {
            return this.mergeJob.Merge();
        } 

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            groupByResults = null;
            if (this.sortJobs != null) resTable = new TableResultsABTree(this.sortJobs[0].tree, this.sortJobs[0].resTable);
            else resTable = new MultiTableResultsRowProxyArray(this.mergeJob.GetTablesOfSortedJobs(), this.mergedResults);
        }

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
            public int[] startIndecesOfRanges;
            
            /// <summary>
            /// Chooses only jobs that had non zero results during search.
            /// </summary>
            public MergeObject(SortJob[] jobs, RowComparer comparer)
            {
                List<SortJob> mergeJobs = new List<SortJob>();
                List<int> startRan = new List<int>();
                this.comparer = comparer;

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
                this.startIndecesOfRanges = startRan.ToArray();
                this.jobsToMerge = mergeJobs.ToArray();
            }

            public List<ITableResults> GetTablesOfSortedJobs()
            {
                List<ITableResults> resTables = new List<ITableResults>();
                for (int i = 0; i < this.jobsToMerge.Length; i++)
                    resTables.Add(this.jobsToMerge[i].resTable);
                return resTables;
            }

            public int GetStartOfRange(int jobIndex)
            {
                return this.startIndecesOfRanges[jobIndex];
            }
            public int GetRange(int jobIndex)
            {
                if (jobIndex + 1 == this.jobsToMerge.Length) return this.source.Length - this.GetStartOfRange(jobIndex);
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
            /// <param name="arr"> Array where to copy the elements. </param>
            /// <returns> A number of copied elements. </returns>
            private int CopySortJobResultsAndClearTree(int jobIndex, TableResults.RowProxy[] arr)
            {
                var job = this.jobsToMerge[jobIndex];

                int i = this.startIndecesOfRanges[jobIndex];
                foreach (var item in job.tree)
                {
                    arr[i] = job.resTable[item];
                    i++;
                }
                int treeCount = job.tree.Count;
                this.ClearTree(jobIndex);
                return treeCount;
            }
            

            public TableResults.RowProxy[] Merge()
            {
                this.MergeResultsParallel(0, this.jobsToMerge.Length, true);
                return this.destination; 
            }

            /// <summary>
            /// Creates a binary tree. 
            /// The leaf nodes represents copying results of the jobs into an array.
            /// The internal nodes represent merging of the subarrays.
            /// The method is initially called only if the end - start >= 2.
            /// </summary>
            /// <param name="start"> An index of the job, that starts the subarray.</param>
            /// <param name="end"> An index of the job, that ends the subarray.</param>
            /// <param name="srcToDest"> On each tree level, it denotes whether, the subsarrays
            /// should be merged into the destination or source array.
            /// True for source to destination, the opposite otherwise. </param>
            /// <returns> The length of the merged sequence.</returns>
            public int MergeResultsParallel(int start, int end, bool srcToDest)
            {
                // Internal node.
                if (end - start > 3)
                {
                    // Compute middle of the range.
                    int middle = ((end - start) / 2) + start;
                    if (middle % 2 == 1) middle--;

                    Task<int> task = Task<int>.Factory.StartNew(() => MergeResultsParallel(middle, end, !srcToDest));
                    // Current thread work.
                    int leftLength = MergeResultsParallel(start, middle, !srcToDest);
                    int rightLength = task.Result;
                
                    // Merge from source to destination.
                    if (srcToDest)
                    {
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                            this.source,                     // Merge from 
                            this.GetStartOfRange(start),     // Start of the left part
                            leftLength,                      // Left part length
                            this.GetStartOfRange(middle),    // Start of the right part
                            rightLength,                     // Right part length
                            this.destination,                // Merge to
                            this.GetStartOfRange(start),     // Index of the merged result
                            this.comparer);                  // Comparer
                    }
                    // Merge from destination to source.
                    else
                    {
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                            this.destination,                 // Merge from 
                            this.GetStartOfRange(start),      // Start of the left part
                            leftLength,                       // Left part length
                            this.GetStartOfRange(middle),     // Start of the Reft part
                            rightLength,                      // Reft part length
                            this.source,                      // Merge to
                            this.GetStartOfRange(start),      // Index of the merged result
                            this.comparer);                   // Comparer
                    }
                    
                    return leftLength + rightLength;
                }
                // Internal node one level before the leaf level.
                // The length of the range is always either 2 or 3.
                else
                { 
                    var from = this.source;
                    var to = this.destination;
                    if (!srcToDest)
                    {
                        from = this.destination;
                        to = this.source;
                    }

                    if (end - start == 2)
                    {   // Copy the SortJob results into the "from" array.
                        Parallel.Invoke(() => CopySortJobResultsAndClearTree(start, from),
                                        () => CopySortJobResultsAndClearTree(start + 1, from));
                        // Merge the two subarrays into the "to" array.
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                           from,                              // Merge from  
                           this.GetStartOfRange(start),       // Start of the left part 
                           this.GetRange(start),              // Left part length 
                           this.GetStartOfRange(start + 1),   // Start of the right part 
                           this.GetRange(start + 1),          // Right part length 
                           to,                                // Merge to 
                           this.GetStartOfRange(start),       // Index of the merged result 
                           this.comparer);                    // Comparer 
                        return this.GetRange(start) + this.GetRange(start + 1);
                    }
                    else if (end - start == 3) 
                    {
                        // It needs to leave the function with all the elements in the "to" array.
                        // To do that it firstly copies the first 2 into the "to" array and the third one to the "from"
                        // Now it will merge the 2 from "to" into "from" and lastly merge the merged result of the 2 with the third one from "from" to "to".

                        // Copy the first two SortJob results into the "to" and the third one to "from".
                        Parallel.Invoke(() => CopySortJobResultsAndClearTree(start, to),
                                        () => CopySortJobResultsAndClearTree(start + 1, to),
                                        () => CopySortJobResultsAndClearTree(start + 2, from));

                        // First merge  the first two from "to" to "from".
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                          to,                                    // Merge from 
                          this.GetStartOfRange(start),           // Start of the left part
                          this.GetRange(start),                  // Left part length
                          this.GetStartOfRange(start + 1),       // Start of the right part
                          this.GetRange(start + 1),              // Right part length
                          from,                                  // Merge to
                          this.GetStartOfRange(start),           // Index of the merged result
                          this.comparer);                        // Comparer
                        // Then merge the result of the above two with the third one into the "to".
                        HPCsharp.ParallelAlgorithm.MergePar<TableResults.RowProxy>(
                          from,                                             // Merge from 
                          this.GetStartOfRange(start),                      // Start of the left part
                          this.GetRange(start) + this.GetRange(start + 1),  // Left part length
                          this.GetStartOfRange(start + 2),                  // Start of the right part
                          this.GetRange(start + 2),                         // Right part length
                          to,                                               // Merge to
                          this.GetStartOfRange(start),                      // Index of the merged result
                          this.comparer);                                   // Comparer
                        // The merged result of the above three will now be in the "to" array.
                        return this.GetRange(start) + this.GetRange(start + 1) + this.GetRange(start + 2);
                    }
                    else throw new ArgumentException($"{this.GetType()}, a division of the ranges went wrong... end - start = {end - start}, but it must be either 2 or 3.");
                }
            }
        }

        private class SortJob
        {
            public ABTree<int> tree;
            public ITableResults resTable;

            public SortJob(IComparer<int> comparer, ITableResults resTable)
            {
                this.tree = new ABTree<int>(256, comparer);
                this.resTable = resTable;
            }
        }
    }
}
