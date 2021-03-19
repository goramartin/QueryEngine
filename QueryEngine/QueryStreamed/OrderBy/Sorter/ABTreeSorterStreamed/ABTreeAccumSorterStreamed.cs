using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// This version of the streamed sorted is using ABTreeValueAccumulator.
    /// The solution should provide a better performance on the graphs where |V| << |E| than the solution with general ABTree, assuming graph G = (V,E) 
    /// and that the number of generated results from pattern matching is  much greater than the number of appearing values. This tells us that the results being sorted are repetitive.
    /// Thus, it only stores each value once and the rest of results with repetitive values are inserted into a corresponding List<int>, therefore saving
    /// time for comparison and space complexity of the tree. 
    /// </summary>
    internal class ABTreeAccumSorterStreamed<T> : ABTreeStreamedSorter<T>
    {
        public ABTreeAccumSorterStreamed(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars) : base(comparers, executionHelper, columnCount, usedVars, true)
        { }

        protected override RangeBucket CreateBucket(IComparer<int> comparer, ITableResults resTable)
        {
            return new RangeBucketAccumTree(comparer, resTable);
        }

        private class RangeBucketAccumTree : RangeBucket
        {
            public RangeBucketAccumTree(IComparer<int> comparer, ITableResults resTable)
            {
                this.tree = new ABTreeValueAccumulator<int>(256, comparer);
                this.resTable = resTable;
            }
        }
        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            groupByResults = null;
            if (this.executionHelper.ThreadCount == 1)
                resTable = new TableResultsABTreeAccum((ABTreeValueAccumulator<int>)this.rangeBuckets[0].tree, this.rangeBuckets[0].resTable);
            else
            {
                TableResultsABTreeAccum[] tmpResults = new TableResultsABTreeAccum[this.rangeBuckets.Length];
                for (int i = 0; i < this.rangeBuckets.Length; i++)
                    tmpResults[i] = (new TableResultsABTreeAccum((ABTreeValueAccumulator<int>)this.rangeBuckets[i].tree, this.rangeBuckets[i].resTable));
                
                resTable = new MultiTableResultsABTree(tmpResults, this.firstKeyComparers[0].isAscending);
            }
        }

    }
}
