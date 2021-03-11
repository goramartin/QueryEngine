using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    ///  This class is a reference single thread solution using Lists as the aggregate result storages.
    ///  The results are grouped using Dictionary, the key is a struct containing a proxy to a row of the result table and its hash.
    ///  The hash is stored inside the key, because the interface of the Dictionary does two accesses.
    ///  Also, the hasher stores cache of the comparer in the Dictionary.
    ///  The value for a group in the dictionary is an index. The index is a position of a group's values inside aggregate result storage.
    /// </summary>
    internal class GroupByWithLists : Grouper
    {
        public GroupByWithLists(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper, false)
        {}

        public override GroupByResults Group(ITableResults resTable)
        {
            //if (this.InParallel) throw new ArgumentException($"{this.GetType()}, cannot perform a parallel group by.");

            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers);
            return this.SingleThreadGroupBy(new RowHasher(hashers), RowEqualityComparerGroupKey.Factory(resTable, comparers, true), resTable);
        }

        /// <summary>
        /// Creates groups and computes aggregate values for each group.
        /// </summary>
        /// <param name="equalityComparer"> Equality comparer where T is group key and computes internaly the hash for each row from the result table.</param>
        /// <param name="resTable"> A result table from the matching clause.</param>
        /// <param name="hasher"> Hasher of rows. </param>
        /// <returns> Aggregate results. </returns>
        private GroupByResults SingleThreadGroupBy(RowHasher hasher, RowEqualityComparerGroupKey equalityComparer, ITableResults resTable)
        {
            #region DECL
            hasher.SetCache(equalityComparer.comparers);
            var aggResults = AggregateListResults.CreateListResults(this.aggregates);
            var groups = new Dictionary<GroupDictKey, int>(equalityComparer);
            int position;
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = 0; i < resTable.NumberOfMatchedElements; i++)
            {
                row = resTable[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.
                if (!groups.TryGetValue(key, out position))
                {
                    position = groups.Count;
                    groups.Add(key, position);
                }
                
                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].Apply(in row, aggResults[j], position);
            }

            return new GroupByResultsList(groups, aggResults, resTable);
        }
    }
}
