using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class MultiGroupGrouper : Grouper
    {
        public MultiGroupGrouper(List<Aggregate> aggs, IGroupByExecutionHelper helper) : base(aggs, helper) { }

        public override List<Aggregate> Group(ITableResults resTable)
        {
            throw new NotImplementedException();
        }
    }
}
