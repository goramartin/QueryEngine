using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// The solution uses general ABTree for sorting incoming results from the pattern matching.
    /// </summary>
    internal class ABTreeGenSorterStreamed<T> : ABTreeStreamedSorter<T>
    {
        public ABTreeGenSorterStreamed(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars) : base(comparers, executionHelper, columnCount, usedVars, false)
        { }

        protected override RangeBucket CreateBucket(IComparer<int> comparer, ITableResults resTable)
        {
            return new RangeBucketGenTree(comparer, resTable);
        }

        private class RangeBucketGenTree : RangeBucket
        {
            public RangeBucketGenTree(IComparer<int> comparer, ITableResults resTable)
            {
                this.tree = new ABTree<int>(256, comparer);
                this.resTable = resTable;
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            groupByResults = null;
            if (this.executionHelper.ThreadCount == 1)
                resTable = new TableResultsABTree((ABTree<int>)this.rangeBuckets[0].tree, this.rangeBuckets[0].resTable);
            else
            {
                TableResultsABTree[] tmpResults = new TableResultsABTree[this.rangeBuckets.Length];
                for (int i = 0; i < this.rangeBuckets.Length; i++)
                    tmpResults[i] = (new TableResultsABTree((ABTree<int>)this.rangeBuckets[i].tree, this.rangeBuckets[i].resTable));
                resTable = new MultiTableResultsABTree(tmpResults, this.firstKeyComparers[0].isAscending);
            }
        }
    }
}
