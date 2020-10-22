using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal abstract class Grouper
    {
        protected Aggregate[] aggregates { get; }
        protected bool InParallel { get; }

        public abstract void Group(ITableResults resTable);
    }
}
