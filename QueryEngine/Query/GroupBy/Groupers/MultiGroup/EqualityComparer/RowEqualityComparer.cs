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
        public ITableResults results;
        public List<ExpressionEqualityComparer> comparers;
        public bool Equals(GroupDictKey x, GroupDictKey y)
        {
            for (int i = 0; i < this.comparers.Count; i++)
                if (!this.comparers[i].Equals(results[x.position], results[y.position])) return false;

            return true;
        }

        public int GetHashCode(GroupDictKey obj)
        {
            return obj.hash;
        }
    }
}
