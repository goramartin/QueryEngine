using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class LocalGroupLocalMerge : Grouper
    {
       // protected RowHasher hasher;
       // protected RowEqualityComparer equalityComparer;

        public LocalGroupLocalMerge(List<Aggregate> aggs, IGroupByExecutionHelper helper) : base(aggs, helper) { }

        public override List<Aggregate> Group(ITableResults resTable)
        {
            throw new NotImplementedException();
        }
    }
}
