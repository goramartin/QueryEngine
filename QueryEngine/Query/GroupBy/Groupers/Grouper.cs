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

        public abstract GroupByResults Group(ITableResults resTable);

        /// <summary>
        /// Creates a list of hashers and comparers.
        /// Notice that the number of comparers is equal to the number of hashers.
        /// Also, they must be of the same generic type.
        /// </summary>
        protected virtual void CreateHashersAndComparers(out List<ExpressionEqualityComparer> comparers, out List<ExpressionHasher> hashers)
        {
            comparers = new List<ExpressionEqualityComparer>();
            hashers = new List<ExpressionHasher>();
            for (int i = 0; i < this.hashes.Count; i++)
            {
                comparers.Add(ExpressionEqualityComparer.Factory(this.hashes[i], this.hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(this.hashes[i], this.hashes[i].ExpressionType));
            }
        }

        public static Grouper Factory(string grouperAlias, List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, bool bucketStorage)
        {
            if (grouperAlias == "ref" && bucketStorage) return new GroupWithBuckets(aggs, hashes, helper);
            else if (grouperAlias == "ref" && !bucketStorage) return new GroupWithLists(aggs, hashes, helper);
            else if (grouperAlias == "global") return new GlobalGroup(aggs, hashes, helper, bucketStorage);
            else if (grouperAlias == "local") return new LocalGroupLocalMerge(aggs, hashes, helper, bucketStorage);
            else if (grouperAlias == "twoway") return new LocalGroupGlobalMerge(aggs, hashes, helper, bucketStorage);
            else throw new ArgumentException("Grouper, trying to create an unknown grouper.");
        }

        public static Grouper Factory( List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper)
        {
            return Factory(helper.GrouperAlias, aggs, helper, helper, helper.BucketStorage);
        }

    }
}
