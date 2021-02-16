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

        public static Grouper Factory(string grouperAlias, Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper)
        {
            if (grouperAlias == "refB") return new GroupWithBuckets(aggs, hashes, helper);
            else if (grouperAlias == "refL") return new GroupWithLists(aggs, hashes, helper);
            else if (grouperAlias == "globalB") return new GlobalGroup(aggs, hashes, helper, true);
            else if (grouperAlias == "globalL") return new GlobalGroup(aggs, hashes, helper, false);
            else if (grouperAlias == "localB") return new LocalGroupLocalMerge(aggs, hashes, helper, true);
            else if (grouperAlias == "localL") return new LocalGroupLocalMerge(aggs, hashes, helper, false);
            else if (grouperAlias == "twowayB") return new LocalGroupGlobalMerge(aggs, hashes, helper, true);
            else if (grouperAlias == "twowayL") return new LocalGroupGlobalMerge(aggs, hashes, helper, false);
            else throw new ArgumentException("Grouper, trying to create an unknown grouper.");
        }

        public static Grouper Factory(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper)
        {
            return Factory(helper.GrouperAlias, aggs, hashes, helper);
        }

    }
}
