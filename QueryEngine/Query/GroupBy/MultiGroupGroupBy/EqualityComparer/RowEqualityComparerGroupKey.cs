using System;
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
    /// </summary>
    internal class RowEqualityComparerGroupKey : IEqualityComparer<GroupDictKey>
    {
        public ITableResults resTable;
        public ExpressionComparer[] comparers;
        public readonly bool cacheResults;
        
        private RowEqualityComparerGroupKey(ITableResults resTable, ExpressionComparer[] expressionComparers, bool cacheResults)
        {
            if (expressionComparers == null || expressionComparers.Length == 0)
                throw new ArgumentException($"{this.GetType()}, trying to assign null to a constructor.");

            this.resTable = resTable;
            this.comparers = expressionComparers;
            this.cacheResults = cacheResults;
        }

        public bool Equals(GroupDictKey x, GroupDictKey y)
        {
            for (int i = 0; i < this.comparers.Length; i++)
                if (this.comparers[i].Compare(this.resTable[x.position], this.resTable[y.position]) != 0) return false;

            return true;
        }

        public int GetHashCode(GroupDictKey key)
        {
            return key.hash;
        }

        /// <summary>
        /// Factory method that creates the comparer by cloning the provided comparers.
        /// </summary>
        public static RowEqualityComparerGroupKey Factory(ITableResults resTable, ExpressionComparer[] comparers, bool cacheResults)
        {
            return new RowEqualityComparerGroupKey(resTable, comparers.CloneHard(cacheResults), cacheResults);
        }

        public RowEqualityComparerGroupKey Clone(bool cacheResults)
        {
            return new RowEqualityComparerGroupKey(this.resTable, comparers.CloneHard(cacheResults), cacheResults);
        }
    }
}
