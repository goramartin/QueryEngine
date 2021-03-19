using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class ABTreeAccumSorterHalfStreamed : ABTreeSorterHalfStreamed
    {
        /// <summary>
        /// If it runs in parallel, the final results will be stored inside.
        /// </summary>
        private RowProxyAccum[] mergedResults;
        private MergeObjectValueAccum mergeJob;

        public ABTreeAccumSorterHalfStreamed(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars) : base(comparers, executionHelper, columnCount, usedVars, true)
        { }

        protected override void MergeResuls()
        {
            this.mergeJob = new MergeObjectValueAccum(this.sortJobs, new RowProxyAccumToRowProxyComparer(RowComparer.Factory(this.comparers, false)));
            if (this.mergeJob.jobsToMerge.Length >= 2)
            {
                this.sortJobs = null;
                this.mergedResults = this.mergeJob.Merge();
            }
            else if (this.mergeJob.jobsToMerge.Length == 1) this.sortJobs = this.mergeJob.jobsToMerge;
            else this.sortJobs = new SortJob[] { this.sortJobs[0] };
        }

        private class MergeObjectValueAccum : MergeObject<RowProxyAccum>
        {
            public MergeObjectValueAccum(SortJob[] jobs, Comparer<RowProxyAccum> comparer) : base(jobs, comparer)
            { }

            protected override int CopySortJobResultsAndClearTree(int jobIndex, RowProxyAccum[] arr)
            {
                var job = this.jobsToMerge[jobIndex];
                int i = this.startIndecesOfRanges[jobIndex];

                var castedTree = (ABTreeValueAccumulator<int>)job.tree;
                foreach (var item in castedTree)
                {
                    arr[i] = new RowProxyAccum(job.resTable[item.value], item.accumulation);
                    i++;
                }
                int treeCount = job.tree.Count;
                this.ClearTree(jobIndex);
                return treeCount;
            }
        }

        protected override SortJob CreateJob(IComparer<int> comparer, ITableResults resTable)
        {
            return new SortJobGenABTreeAccum(comparer, resTable);
        }

        private class SortJobGenABTreeAccum : SortJob
        {
            public SortJobGenABTreeAccum(IComparer<int> comparer, ITableResults resTable)
            {
                this.tree = new ABTreeValueAccumulator<int>(256, comparer);
                this.resTable = resTable;
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            groupByResults = null;
            if (this.sortJobs != null) resTable = new TableResultsABTreeAccum((ABTreeValueAccumulator<int>)this.sortJobs[0].tree, this.sortJobs[0].resTable);
            else resTable = new MultiTableResultsRowProxyAccum(this.mergeJob.GetTablesOfSortedJobs(), this.mergedResults);
        }
    }
}
