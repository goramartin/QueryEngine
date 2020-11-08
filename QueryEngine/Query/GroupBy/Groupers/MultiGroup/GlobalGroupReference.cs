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
    internal class GlobalMergeReference : Grouper
    {
        private List<AggregateBucket> bucketAggregates = null;
        public GlobalMergeReference(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        { }

        public override List<AggregateArrayResults> Group(ITableResults resTable)
        {
            // Create bucket aggregates
            this.bucketAggregates = new List<AggregateBucket>();
            for (int i = 0; i < this.aggregates.Count; i++)
                this.bucketAggregates.Add((AggregateBucket)Aggregate.FactoryBucketType(this.aggregates[i]));

            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            var equalityComparers = new List<ExpressionEqualityComparer>();
            var hashers = new List<ExpressionHasher>();
            for (int i = 0; i < hashes.Count; i++)
            {
                equalityComparers.Add(ExpressionEqualityComparer.Factory(hashes[i], hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(hashes[i], hashes[i].ExpressionType, null));
            }

            this.GroupWork(new RowEqualityComparerWithHash(resTable, equalityComparers, new RowHasher(hashers), true), resTable);

            return null;
        }

        private List<AggregateBucketResult> GroupWork(RowEqualityComparerWithHash equalityComparer, ITableResults results)
        {
            AggregateBucketResult[] buckets = null; 
            var groups = new Dictionary<int, AggregateBucketResult[]>(equalityComparer);
            TableResults.RowProxy row;

            // Create groups and compute aggregates for each individual group.
            for (int i = 0; i < results.NumberOfMatchedElements; i++)
            {
                row = results[i];
                if (!groups.TryGetValue(i, out buckets))
                {
                    buckets = AggregateBucketResult.CreateBucketResults(this.bucketAggregates);
                    groups.Add(i, buckets);
                }

                for (int j = 0; j < this.bucketAggregates.Count; j++)
                    this.bucketAggregates[j].Apply(in row, buckets[j]);
            }

            return null;
        }
    }
}
