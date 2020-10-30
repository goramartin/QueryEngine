using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class is a base class for each grouper.
    /// A grouper is a class that groups results from the search query into groups.
    /// There are two groupers. A single group grouper which represents a grouping when an aggregate is used
    /// in the query but no group by is set.S
    /// The other grouper is a multi group grouper that covers the grouping otherwise.
    /// </summary>
    internal abstract class Grouper
    {
        protected List<Aggregate> aggregates { get; }
        protected bool InParallel { get; }

        protected int ThreadCount { get; }

        protected Grouper(List<Aggregate> aggs, IGroupByExecutionHelper helper)
        {
            this.ThreadCount = helper.ThreadCount;
            this.aggregates = aggs;
            this.InParallel = helper.InParallel;
        }

        public abstract List<Aggregate> Group(ITableResults resTable);
    }
}
