using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    internal class RowEqualityComparerNoHash : IEqualityComparer<GroupDictKey>
    {
        public ITableResults Results { get; }
        public List<ExpressionEqualityComparer> Comparers { get; }

        public RowEqualityComparerNoHash(ITableResults results, List<ExpressionEqualityComparer> comparers)
        {
            this.Results = results;
            this.Comparers = comparers;
        }

        public bool Equals(GroupDictKey x, GroupDictKey y)
        {
            for (int i = 0; i < this.Comparers.Count; i++)
                if (!this.Comparers[i].Equals(Results[x.position], Results[y.position])) return false;

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
        public RowEqualityComparerNoHash Clone()
        {
            var tmp = new List<ExpressionEqualityComparer>();
            for (int i = 0; i < this.Comparers.Count; i++)
                tmp.Add(this.Comparers[i].Clone());

            return new RowEqualityComparerNoHash(this.Results, tmp);
        }
    }
}
