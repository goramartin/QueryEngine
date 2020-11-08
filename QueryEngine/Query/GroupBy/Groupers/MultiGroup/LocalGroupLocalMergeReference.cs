using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    ///  This class is a reference single thread solution to the LocalGroupLocalMerge solution.
    ///  It works the same as LocalGroupLocalMerge solution, except, it uses solely integer key into the dictionary.
    /// </summary>
    internal class LocalGroupLocalMergeReference : Grouper
    {
        public LocalGroupLocalMergeReference(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        { }

        public override List<AggregateArrayResults> Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            var equalityComparers = new List<ExpressionEqualityComparer>();
            var hashers = new List<ExpressionHasher>();
            for (int i = 0; i < hashes.Count; i++)
            {
                equalityComparers.Add(ExpressionEqualityComparer.Factory(hashes[i], hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(hashes[i], hashes[i].ExpressionType, null));
            }

            return this.GroupWork(new RowEqualityComparerWithHash(resTable, equalityComparers, new RowHasher(hashers), true),resTable);
        }

        /// <summary>
        /// Creates groups and computes aggregate values for each group.
        /// </summary>
        /// <param name="equalityComparer"> Equality comparer where T is int and computes internaly the hash for each row from the result table.</param>
        /// <returns> Aggregate results. </returns>
        private List<AggregateArrayResults> GroupWork(RowEqualityComparerWithHash equalityComparer, ITableResults results)
        {
            var aggResults = AggregateArrayResults.CreateArrayResults(this.aggregates);
            var groups = new Dictionary<int, int>(equalityComparer);
            int position;
            TableResults.RowProxy row;

            // Set internal results of the aggregates.
            for (int i = 0; i < this.aggregates.Count; i++)
                this.aggregates[i].SetAggResults(aggResults[i]);

            // Create groups and compute aggregates for each individual group.
            for (int i = 0; i < results.NumberOfMatchedElements; i++)
            {
                row = results[i];
                if (!groups.TryGetValue(i, out position))
                {
                    position = groups.Count;
                    groups.Add(i, position);
                }

                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in row, position);
            }

            return aggResults;
        }
    }
}
