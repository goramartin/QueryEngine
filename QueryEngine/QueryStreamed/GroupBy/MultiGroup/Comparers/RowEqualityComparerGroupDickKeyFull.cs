using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A EqualityComparer that is used during half streamed group by.
    /// The full key contains full row proxy and it is hash values.
    /// The row proxies are comparer for the key equality.
    /// </summary>
    class RowEqualityComparerGroupDickKeyFull : IEqualityComparer<GroupDictKeyFull>
    {
        public ExpressionEqualityComparer[] Comparers { get; }

        public RowEqualityComparerGroupDickKeyFull(ExpressionEqualityComparer[] comparers)
        {
            this.Comparers = comparers;
        }

        public bool Equals(GroupDictKeyFull x, GroupDictKeyFull y)
        {
            for (int i = 0; i < this.Comparers.Length; i++)
                if (!this.Comparers[i].Equals(x.row, y.row)) return false;

            return true;
        }

        public int GetHashCode(GroupDictKeyFull obj)
        {
           return obj.hash;
        }
    }
}
