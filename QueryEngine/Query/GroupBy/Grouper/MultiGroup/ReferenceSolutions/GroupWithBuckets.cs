using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// This class is a reference single thread solution to the GlobalGroup solution.
    /// It works the same except it uses simple dictionary.
    /// </summary>
    internal class GroupWithBuckets : Grouper
    {
        public GroupWithBuckets(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper, true)
        {}

        public override GroupByResults Group(ITableResults resTable)
        {
            //if (this.InParallel) throw new ArgumentException($"{this.GetType()}, cannot perform a parallel group by.");

            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers);
            return this.SingleThreadGroupBy(new RowHasher(hashers), RowEqualityComparerGroupKey.Factory(resTable, comparers, true), resTable);
        }

        private GroupByResults SingleThreadGroupBy(RowHasher hasher, RowEqualityComparerGroupKey comparer, ITableResults resTable)
        {
            #region DECL
            hasher.SetCache(comparer.comparers);
            AggregateBucketResult[] buckets = null; 
            var groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>(comparer);
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = 0; i < resTable.NumberOfMatchedElements; i++)
            {
                row = resTable[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.
                if (!groups.TryGetValue(key, out buckets))
                {
                    buckets = AggregateBucketResult.CreateBucketResults(this.aggregates);
                    groups.Add(key, buckets);
                }

                for (int j = 0; j < this.aggregates.Length; j++)
                    this.aggregates[j].Apply(in row, buckets[j]);
            }

            return new DictGroupDictKeyBucket(groups, resTable);
        }
    }
}
