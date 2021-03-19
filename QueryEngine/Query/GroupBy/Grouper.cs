using System;

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
        protected Aggregate[] aggregates { get; }
        protected ExpressionHolder[] hashes { get; }
        protected bool InParallel { get; }
        protected int ThreadCount { get; }
        protected bool BucketStorage { get; set; }

        protected Grouper(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool bucketStorage)
        {
            if (helper == null || aggs == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

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
        protected virtual void CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers)
        {
            comparers = new ExpressionComparer[this.hashes.Length];
            hashers = new ExpressionHasher[this.hashes.Length];
            for (int i = 0; i < this.hashes.Length; i++)
            {
                comparers[i] = (ExpressionComparer.Factory(this.hashes[i], true, false)); // hash, ascending, no cache.
                hashers[i] = (ExpressionHasher.Factory(this.hashes[i]));
            }
        }

        public static Grouper Factory(GrouperAlias grouperAlias, Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper)
        {
            if (grouperAlias == GrouperAlias.RefB) return new GroupByWithBuckets(aggs, hashes, helper);
            else if (grouperAlias == GrouperAlias.RefL) return new GroupByWithLists(aggs, hashes, helper);
            else if (grouperAlias == GrouperAlias.GlobalB) return new GlobalGroupByBucket(aggs, hashes, helper, true);
            else if (grouperAlias == GrouperAlias.GlobalL) return new GlobalGroupByArray(aggs, hashes, helper, false);
            else if (grouperAlias == GrouperAlias.LocalB) return new LocalGroupByLocalTwoWayMergeBucket(aggs, hashes, helper, true);
            else if (grouperAlias == GrouperAlias.LocalL) return new LocalGroupByLocalTwoWayMergeList(aggs, hashes, helper, false);
            else if (grouperAlias == GrouperAlias.TwoStepB) return new TwoStepGroupByBucket(aggs, hashes, helper, true);
            else if (grouperAlias == GrouperAlias.TwoStepL) return new TwoStepGroupByListBucket(aggs, hashes, helper, false);
            else throw new ArgumentException("Grouper, trying to create an unknown grouper.");
        }

        public static Grouper Factory(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper)
        {
            return Factory(helper.GrouperAlias, aggs, hashes, helper);
        }

    }
}
