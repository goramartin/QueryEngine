using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    class RowEqualityComparerGroupDickKeyFull : IEqualityComparer<GroupDictKeyFull>
    {
        public List<ExpressionEqualityComparer> Comparers { get; }

        public RowEqualityComparerGroupDickKeyFull(List<ExpressionEqualityComparer> comparers)
        {
            this.Comparers = comparers;
        }

        public bool Equals(GroupDictKeyFull x, GroupDictKeyFull y)
        {
            for (int i = 0; i < this.Comparers.Count; i++)
                if (!this.Comparers[i].Equals(x.row, y.row)) return false;

            return true;
        }

        public int GetHashCode(GroupDictKeyFull obj)
        {
           return obj.hash;
        }
    }
}
