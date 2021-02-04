using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A class serves as a input for dictionary when performing multi group grouping.
    /// This class does not compute hash function because the hash is expected to be stored
    /// in the GrouDictKey struct that is the key in the dictionary.
    /// 
    /// This class is connected to the hasher classes via comparers (Hasher have stored reference to the given comparers).
    /// It is done because during the computation of the hash the y parameter is then compared with the x n-times. 
    /// When inserting it into the dictionary, it will again trigger computation of the same value that the hash was calculated from.
    /// Thus, we want to avoid unneccessary computation of expressions again for the same row y.
    /// 
    /// Is used by LocalGroupLocalMerge.
    /// </summary>
    internal class RowEqualityComparerGroupKey : IEqualityComparer<GroupDictKey>
    {
        public ITableResults ResTable { get; set; }
        public ExpressionEqualityComparer[] Comparers { get; }
        public bool CacheOn { get; private set; }
        
        public RowEqualityComparerGroupKey(ITableResults resTable, ExpressionEqualityComparer[] comparers)
        {
            this.ResTable = resTable;
            this.Comparers = comparers;
        }

        public bool Equals(GroupDictKey x, GroupDictKey y)
        {
            for (int i = 0; i < this.Comparers.Length; i++)
                if (!this.Comparers[i].Equals(this.ResTable[x.position], this.ResTable[y.position])) return false;

            return true;
        }

        public int GetHashCode(GroupDictKey key)
        {
            return key.hash;
        }

        /// <summary>
        /// Clone the entire equality comparer.
        /// It clones also the comparers because they contain cache.
        /// </summary>
        public RowEqualityComparerGroupKey Clone()
        {
            var tmp = new ExpressionEqualityComparer[this.Comparers.Length];
            for (int i = 0; i < this.Comparers.Length; i++)
                tmp[i] = (this.Comparers[i].Clone());

            return new RowEqualityComparerGroupKey(this.ResTable, tmp);
        }

        public void SetCache(RowHasher hasher)
        {
            this.CacheOn = true;
            for (int i = 0; i < this.Comparers.Length; i++)
                this.Comparers[i].SetCache(hasher.Hashers[i]);
        }

        public void UnsetCache()
        {
            this.CacheOn = false ;
            for (int i = 0; i < this.Comparers.Length; i++)
                this.Comparers[i].SetCache(null);
        }
    }
}
