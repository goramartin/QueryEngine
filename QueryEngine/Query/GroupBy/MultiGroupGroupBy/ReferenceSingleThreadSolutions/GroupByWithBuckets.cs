using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    ///  This class is a reference single thread solution using Lists as the aggregate result storages.
    ///  The results are grouped using Dictionary, the key is a struct containing a proxy to a row of the result table and its hash.
    ///  The hash is stored inside the key, because the interface of the Dictionary does two accesses.
    ///  Also, the hasher stores cache of the comparer in the Dictionary.
    ///  The aggregate values of the groups are stored as values in the Dictionary, unlike the GroupBy with Lists.
    ///  The value it self is an array of value holders.
    /// </summary>
    internal class GroupByWithBuckets : Grouper
    {
        public GroupByWithBuckets(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper, true)
        {}

        public override GroupByResults Group(ITableResults resTable)
        {
            //if (this.InParallel) throw new ArgumentException($"{this.GetType()}, cannot perform a parallel group by.");

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
