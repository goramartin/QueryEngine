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

        public override AggregateResults Group(ITableResults resTable)
        {
            if (this.InParallel) throw new ArgumentException($"{this.GetType()}, cannot perform a parallel group by.");
            
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            var equalityComparers = new List<ExpressionEqualityComparer>();
            var hashers = new List<ExpressionHasher>();
            for (int i = 0; i < hashes.Count; i++)
            {
                equalityComparers.Add(ExpressionEqualityComparer.Factory(hashes[i], hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(hashes[i], hashes[i].ExpressionType));
            }

            return this.SingleThreadGroupBy(new RowEqualityComparerInt(resTable, equalityComparers, new RowHasher(hashers), true), resTable);
        }

        private AggregateResults SingleThreadGroupBy(RowEqualityComparerInt equalityComparer, ITableResults results)
        {
            #region DECL
            AggregateBucketResult[] buckets = null; 
            var groups = new Dictionary<int, AggregateBucketResult[]>(equalityComparer);
            TableResults.RowProxy row;
            #endregion DECL
            // Create groups and compute aggregates for each individual group.
            for (int i = 0; i < results.NumberOfMatchedElements; i++)
            {
                row = results[i];
                if (!groups.TryGetValue(i, out buckets))
                {
                    buckets = AggregateBucketResult.CreateBucketResults(this.aggregates);
                    groups.Add(i, buckets);
                }

                for (int j = 0; j < this.aggregates.Count; j++)
                    this.aggregates[j].Apply(in row, buckets[j]);
            }


            return null;
        }
    }
}
