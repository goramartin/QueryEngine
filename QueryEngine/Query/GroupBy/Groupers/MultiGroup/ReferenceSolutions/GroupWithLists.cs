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
    internal class GroupWithLists : Grouper
    {
        public GroupWithLists(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper, false)
        {}

        public override AggregateResults Group(ITableResults resTable)
        {
            if (this.InParallel) throw new ArgumentException($"{this.GetType()}, cannot perform a parallel group by.");

            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out List<ExpressionEqualityComparer> equalityComparers, out List<ExpressionHasher> hashers);
            return this.SingleThreadGroupBy(new RowHasher(hashers), new RowEqualityComparerGroupKey(resTable, equalityComparers), resTable);
        }

        /// <summary>
        /// Creates groups and computes aggregate values for each group.
        /// </summary>
        /// <param name="equalityComparer"> Equality comparer where T is group key and computes internaly the hash for each row from the result table.</param>
        /// <param name="results"> A result table from the matching clause.</param>
        /// <param name="hasher"> Hasher of rows. </param>
        /// <returns> Aggregate results. </returns>
        private AggregateResults SingleThreadGroupBy(RowHasher hasher, RowEqualityComparerGroupKey equalityComparer, ITableResults results)
        {
            #region DECL
            equalityComparer.SetCache(hasher);
            hasher.SetCache(equalityComparer.Comparers);
            var aggResults = AggregateListResults.CreateArrayResults(this.aggregates);
            var groups = new Dictionary<GroupDictKey, int>(equalityComparer);
            int position;
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            // Create groups and compute aggregates for each individual group.
            for (int i = 0; i < results.NumberOfMatchedElements; i++)
            {
                row = results[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.
                if (!groups.TryGetValue(key, out position))
                {
                    position = groups.Count;
                    groups.Add(key, position);
                }

                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in row, aggResults[j], position);
            }

            // return aggResults;
            return null;
        }
    }
}
