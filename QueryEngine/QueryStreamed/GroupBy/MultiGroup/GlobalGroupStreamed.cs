using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class GlobalGroupStreamed : GroupResultProcessor
    {

        public GlobalGroupStreamed(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, int columnCount) : base(aggs, hashes, helper, columnCount)
        {

        }

        public override void Process(int matcherID, Element[] result)
        {
            if (this.InParallel) ProcessParallel(matcherID, result);
            else ProcessSingleThread(matcherID, result);
        }


        private void ProcessSingleThread(int matcherID, Element[] result)
        {

        }

        private void ProcessParallel(int matcherID, Element[] result)
        {




        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            throw new NotImplementedException();
        }
    }
}
