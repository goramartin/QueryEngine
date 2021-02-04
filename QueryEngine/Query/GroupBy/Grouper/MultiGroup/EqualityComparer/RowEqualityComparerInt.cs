using System.Collections.Generic;

namespace QueryEngine 
{
    /// <summary>
    /// A class serves as a input for dictionary when performing multi group grouping.
    /// This class does compute hash because its main purpose is in the GlobalMerge grouping, where
    /// there is no need for the GroupDictKey, because no merging is done and the value of the hash
    /// is not needed futher.
    /// Is used in GlobalMerge.
    /// </summary>
    internal class RowEqualityComparerInt : IEqualityComparer<int>
    {
        public ITableResults ResTable { get; }
        public ExpressionEqualityComparer[] Comparers { get; }
        public RowHasher Hasher { get; }

        /// <summary>
        /// Construst equality comparer.
        /// </summary>
        /// <param name="CacheOn"> The cache signals, whether the given hasher should cache results for the comparers.</param>
        public RowEqualityComparerInt(ITableResults resTable, ExpressionEqualityComparer[] comparers, RowHasher hasher, bool CacheOn)
        {
            this.ResTable = resTable;
            this.Comparers = comparers;
            this.Hasher = hasher;

            // Just in case, set the cache to null.
            if (CacheOn)
            {
                this.SetCache();
                this.Hasher.SetCache(this.Comparers);
            }
            else
            {
                this.UnsetCache();
                this.Hasher.UnsetCache();
            }
        }

        public bool Equals(int x, int y)
        {
            for (int i = 0; i < this.Comparers.Length; i++)
                if (!this.Comparers[i].Equals(this.ResTable[x], this.ResTable[y])) return false;

            return true;
        }

        public int GetHashCode(int obj)
        {
            return this.Hasher.Hash(this.ResTable[obj]);
        }

        private void SetCache()
        {
            for (int i = 0; i < this.Comparers.Length; i++)
                this.Comparers[i].SetCache(this.Hasher.Hashers[i]);
        }

        private void UnsetCache()
        {
            for (int i = 0; i < this.Comparers.Length; i++)
                this.Comparers[i].SetCache(null);
        }

    }
}
