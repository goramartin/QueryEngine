using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
    /// <summary>
    /// A class serves as a input for dictionary when performing multi group grouping.
    /// This class does compute hash because its main purpose is in the GlobalMerge grouping, where
    /// there is no need for the GroupDictKey, because no merging is done and the value of the hash
    /// is not needed futher.
    /// Is used in GlobalMerge.
    /// </summary>
    internal class RowEqualityComparerWithHash : IEqualityComparer<int>
    {
        public ITableResults Results { get; }
        public List<ExpressionEqualityComparer> Comparers { get; }
        public RowHasher Hasher { get; }

        /// <summary>
        /// Construst equality comparer.
        /// </summary>
        /// <param name="CacheOn"> The cache signals, whether the given hasher should cache results for the comparers.</param>
        public RowEqualityComparerWithHash(ITableResults results, List<ExpressionEqualityComparer> comparers, RowHasher hasher, bool CacheOn)
        {
            this.Results = results;
            this.Comparers = comparers;
            this.Hasher = hasher;

            // Just in case, set the cache to null.
            if (CacheOn) this.Hasher.SetCache(this.Comparers);
            else this.Hasher.UnsetCache();
        }

        public bool Equals(int x, int y)
        {
            for (int i = 0; i < this.Comparers.Count; i++)
                if (!this.Comparers[i].Equals(Results[x], Results[y])) return false;

            return true;
        }

        public int GetHashCode(int obj)
        {
            return this.Hasher.Hash(this.Results[obj]);
        }

    }
}
