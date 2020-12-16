using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// This class is a reference single thread solution to the GlobalGroup solution.
    /// It works the same except it uses simple dictionary.
    /// </summary>
    internal class GroupWithBuckets : Grouper
    {
        public GroupWithBuckets(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper, true)
        {}

        public override GroupByResults Group(ITableResults resTable)
        {
            //if (this.InParallel) throw new ArgumentException($"{this.GetType()}, cannot perform a parallel group by.");

            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out List<ExpressionEqualityComparer> equalityComparers, out List<ExpressionHasher> hashers);
            return this.SingleThreadGroupBy(new RowHasher(hashers), new RowEqualityComparerGroupKey(resTable, equalityComparers), resTable);
        }

        private GroupByResults SingleThreadGroupBy(RowHasher hasher, RowEqualityComparerGroupKey equalityComparer, ITableResults results)
        {
            #region DECL
            hasher.SetCache(equalityComparer.Comparers);
            equalityComparer.SetCache(hasher);
            AggregateBucketResult[] buckets = null; 
            var groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>(equalityComparer);
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = 0; i < results.NumberOfMatchedElements; i++)
            {
                row = results[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.
                if (!groups.TryGetValue(key, out buckets))
                {
                    buckets = AggregateBucketResult.CreateBucketResults(this.aggregates);
                    groups.Add(key, buckets);
                }

                for (int j = 0; j < this.aggregates.Count; j++)
                    this.aggregates[j].Apply(in row, buckets[j]);
            }

            return new DictGroupDictKeyBucket(groups, results);
        }
    }
}
