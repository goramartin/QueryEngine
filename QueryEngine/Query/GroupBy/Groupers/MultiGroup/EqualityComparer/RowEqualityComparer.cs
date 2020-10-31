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
    /// This class is connected to the hasher classes. Because during the computation of the hash
    /// the x parameter is then compared with the y n-times (depends on the lenght of the linked list)
    /// so we want to avoid unneccessary computation of expression again for the same row x.
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
