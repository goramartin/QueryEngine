using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A EqualityComparer that is used during half streamed group by.
    /// The full key contains row proxy and it is hash value.
    /// The row proxies are compared for the key equality.
    /// </summary>
    class RowEqualityComparerGroupDickKeyFull : IEqualityComparer<GroupDictKeyFull>
    {
        public ExpressionComparer[] comparers;
        public readonly bool cacheResults;

        private RowEqualityComparerGroupDickKeyFull(ExpressionComparer[] comparers, bool cacheResults)
        {
            this.comparers = comparers;
            this.cacheResults = cacheResults;
        }

        public bool Equals(GroupDictKeyFull x, GroupDictKeyFull y)
        {
            for (int i = 0; i < this.comparers.Length; i++)
                if (this.comparers[i].Compare(x.row, y.row) != 0) return false;

            return true;
        }

        public int GetHashCode(GroupDictKeyFull obj)
        {
           return obj.hash;
        }

        /// <summary>
        /// Creates a new instance.
        /// The provided comparers are cloned with appropriately set cache flag.
        /// </summary>
        public static RowEqualityComparerGroupDickKeyFull Factory(ExpressionComparer[] comparers, bool cacheResults)
        {
            return new RowEqualityComparerGroupDickKeyFull(comparers.CloneHard(cacheResults), cacheResults);
        }
    }
}
