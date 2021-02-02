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
        private SortJob[] sortJobs;
        private int sortJobsFinished = 0;
        private object globalLock = new object();
        private MergeJob mergeJob;

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
                        this.mergeJob = InitMergeJob();
                        this.MergeResuls();
                    }
                }
            }
        }

        // rec
        private void MergeResuls() 
        { 
        
        
        
        
        } 

        private void MergeResultsRecursion(int start, int end)
        {





        }


        /// <summary>
        /// Copy results of one sortJob into array that will be provided for the Merge methods. 
        /// </summary>
        /// <param name="jobIndex"> Results to copy. </param>
        /// <param name="startIndex"> An index where to start inserting elements to the source array. </param>
        private void CopySortJobResults(int jobIndex)
        {
            var job = this.sortJobs[jobIndex];
            var source = this.mergeJob.source;

            int i = this.mergeJob.startRanges[jobIndex];
            foreach (var item in job.tree)
            {
                source[i] = job.results[item];
                i++;
            }
        }

        private MergeJob InitMergeJob()
        {
            int count = 0;
            int[] startRanges = new int[this.executionHelper.ThreadCount];

            for (int i = 0; i < this.sortJobs.Length; i++)
            {
                if (this.sortJobs[i].tree.Count == 0)
                    startRanges[i] = -1;
                else
                {
                    startRanges[i] = count;
                    count += this.sortJobs[i].tree.Count;
                }
            }
            return new MergeJob(count, startRanges);
        }

        private class MergeJob
        {
            public TableResults.RowProxy[] source;
            public TableResults.RowProxy[] destination;
            public int[] startRanges;

            public MergeJob(int sourceSize, int[] startRanges)
            {
                this.source = new TableResults.RowProxy[sourceSize];
                this.startRanges = startRanges;
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
