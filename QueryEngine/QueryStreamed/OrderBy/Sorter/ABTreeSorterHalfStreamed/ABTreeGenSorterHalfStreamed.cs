using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class ABTreeGenSorterHalfStreamed : ABTreeSorterHalfStreamed
    {
        /// <summary>
        /// If it runs in parallel, the final results will be stored inside.
        /// </summary>
        private TableResults.RowProxy[] mergedResults;
        private MergeObjectRowProxy mergeJob;

        public ABTreeGenSorterHalfStreamed(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars) : base(comparers, executionHelper, columnCount, usedVars, false)
        { }

        protected override void MergeResuls()
        {
            this.mergeJob = new MergeObjectRowProxy(this.sortJobs, RowComparer.Factory(this.comparers, false));
            if (this.mergeJob.jobsToMerge.Length >= 2)
            {
                this.sortJobs = null;
                this.MergeResuls();
            }
            else if (this.mergeJob.jobsToMerge.Length == 1) this.sortJobs = this.mergeJob.jobsToMerge;
            else this.sortJobs = new SortJob[] { this.sortJobs[0] };
        }

        private class MergeObjectRowProxy : MergeObject<TableResults.RowProxy>
        {
            public MergeObjectRowProxy(SortJob[] jobs, Comparer<TableResults.RowProxy> comparer) : base(jobs, comparer)
            { }

            protected override int CopySortJobResultsAndClearTree(int jobIndex, TableResults.RowProxy[] arr)
            {
                var job = this.jobsToMerge[jobIndex];
                int i = this.startIndecesOfRanges[jobIndex];

                var castedTree = (ABTree<int>)job.tree;
                foreach (var item in castedTree)
                {
                    arr[i] = job.resTable[item];
                    i++;
                }
                int treeCount = job.tree.Count;
                this.ClearTree(jobIndex);
                return treeCount;
            }
        }

        protected override SortJob CreateJob(IComparer<int> comparer, ITableResults resTable)
        {
            return new SortJobGenABTree(comparer, resTable);
        }

        private class SortJobGenABTree : SortJob
        {
            public SortJobGenABTree(IComparer<int> comparer, ITableResults resTable)
            {
                this.tree = new ABTree<int>(256, comparer);
                this.resTable = resTable;
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            groupByResults = null;
            if (this.sortJobs != null) resTable = new TableResultsABTree((ABTree<int>)this.sortJobs[0].tree, this.sortJobs[0].resTable);
            else resTable = new MultiTableResultsRowProxyArray(this.mergeJob.GetTablesOfSortedJobs(), this.mergedResults);
        }
    }
}
