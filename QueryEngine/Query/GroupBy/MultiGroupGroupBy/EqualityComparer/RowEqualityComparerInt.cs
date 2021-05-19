using System.Collections.Generic;

namespace QueryEngine 
{
    /// <summary>
    /// A class serves as a input for dictionary when performing multi group grouping.
    /// This class does compute hash because its main purpose is in the Global grouping, where
    /// there is no need for the GroupDictKey, because no merging is done and the value of the hash
    /// is not needed futher.
    /// Is used in Global group by.
    /// </summary>
    internal class RowEqualityComparerInt : IEqualityComparer<int>
    {
        public ITableResults resTable;
        public ExpressionComparer[] Comparers;
        public RowHasher hasher;
        public readonly bool cacheResults;

        private RowEqualityComparerInt(ITableResults resTable, ExpressionComparer[] comparers, RowHasher hasher, bool cacheResults)
        {
            this.resTable = resTable;
            this.Comparers = comparers;
            this.hasher = hasher;
            this.cacheResults = cacheResults;
        }

        public bool Equals(int x, int y)
        {
            for (int i = 0; i < this.Comparers.Length; i++)
                if (this.Comparers[i].Compare(this.resTable[x], this.resTable[y]) != 0) return false;

            return true;
        }

        public int GetHashCode(int obj)
        {
            return this.hasher.Hash(this.resTable[obj]);
        }

        public static RowEqualityComparerInt Factory(ITableResults resTable, ExpressionComparer[] comparers, RowHasher hasher, bool cacheResults)
        {
            var newComparers = comparers.CloneHard(cacheResults);
            
            var newHasher = hasher.Clone();
            if (cacheResults) newHasher.SetCache(newComparers);
            else newHasher.UnsetCache();

            return new RowEqualityComparerInt(resTable, newComparers, newHasher, cacheResults);
        }
    }
}
