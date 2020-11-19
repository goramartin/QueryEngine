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
        protected List<ExpressionHolder> hashes { get; }
        protected bool InParallel { get; }
        protected int ThreadCount { get; }
        protected bool BucketStorage { get; set; }

        protected Grouper(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, bool bucketStorage)
        {
            this.ThreadCount = helper.ThreadCount;
            this.aggregates = aggs;
            this.InParallel = helper.InParallel;
            this.hashes = hashes;
            this.BucketStorage = bucketStorage;
        }

        public abstract AggregateResults Group(ITableResults resTable);

        /// <summary>
        /// Creates a list of hashers and comparers.
        /// Notice that the number of comparers is equal to the number of hashers.
        /// Also, they must be of the same generic type.
        /// </summary>
        public virtual void CreateHashersAndComparers(out List<ExpressionEqualityComparer> comparers, out List<ExpressionHasher> hashers)
        {
            comparers = new List<ExpressionEqualityComparer>();
            hashers = new List<ExpressionHasher>();
            for (int i = 0; i < this.hashes.Count; i++)
            {
                comparers.Add(ExpressionEqualityComparer.Factory(this.hashes[i], this.hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(this.hashes[i], this.hashes[i].ExpressionType));
            }
        }


    }
}
