using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private Job[] localSortJobs;


        public ABTreeHalfStreamedSorter(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount) 
            : base(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount) 
        {
            var tmpComp = new RowComparer(this.comparers);
            this.localSortJobs = new Job[this.executionHelper.ThreadCount];
            for (int i = 0; i < localSortJobs.Length; i++)
            {
                var results = new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize);
                this.localSortJobs[i] = new Job(this.CreateComparer(tmpComp, results), results);
            }
        } 

        public override void Process(int matcherID, Element[] result)
        {
            var tmpJob = this.localSortJobs[matcherID];
            if (result != null)
            {
                tmpJob.results.StoreRow(result);
                tmpJob.tree.Insert(tmpJob.results.RowCount - 1);
            } else
            {
                // Parallel part
                if (this.localSortJobs.Length > 1)
                {

                }
            }
        }


        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            throw new NotImplementedException();
        }

        private class Job
        {
            public ABTree<int> tree;
            public TableResults results;

            public Job(IComparer<int> comparer, TableResults results)
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
    }
}
