using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class ABTreeHalfStreamedSorter : OrderByResultProcessor
    {
        private Job[] matcherJobs;


        public ABTreeHalfStreamedSorter(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount) 
            : base(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount) 
        {
            var tmpComp = new RowComparer(this.comparers);
            this.matcherJobs = new Job[this.executionHelper.ThreadCount];
            for (int i = 0; i < matcherJobs.Length; i++)
            {
                var results = new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize);
                this.matcherJobs[i] = new Job(this.CreateComparer(tmpComp, results), results);
            }
        } 

        public override void Process(int matcherID, Element[] result)
        {
            var tmpJob = this.matcherJobs[matcherID];
            if (result != null)
            {
                tmpJob.results.StoreRow(result);
                tmpJob.tree.Insert(tmpJob.results.RowCount - 1);
            } else
            {
                // Parallel part
                if (this.matcherJobs.Length > 1)
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

        private IndexToRowProxyComparer CreateComparer(RowComparer comparer, TableResults results) 
        {
            var newComparer = comparer.Clone();
            newComparer.SetCaching(true);
            return new IndexToRowProxyComparer(newComparer, results);
        }
    }
}
