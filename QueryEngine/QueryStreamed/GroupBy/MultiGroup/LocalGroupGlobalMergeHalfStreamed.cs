using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{

    internal class LocalGroupGlobalMergeResultProcessorHalfStreamed : GroupResultProcessor
    {

        public LocalGroupGlobalMergeResultProcessorHalfStreamed(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper): base(aggs, hashes, helper)
        {


        }





        public override void Process(int matcherID, Element[] result)
        {




            throw new NotImplementedException();
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {





            throw new NotImplementedException();
        }
    }
}
