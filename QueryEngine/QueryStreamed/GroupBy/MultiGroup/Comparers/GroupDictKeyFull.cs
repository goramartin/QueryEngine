using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class representing a dictionary key in the streamed version of the grouping.
    /// The only difference between the class GroupDictKey is that it contains the full information about the
    /// row and not just a position indicator.
    /// </summary>
    internal class GroupDictKeyFull
    {
        public readonly int hash;
        public readonly TableResults.RowProxy row;

        public GroupDictKeyFull(int hash, TableResults.RowProxy row)
        {
            this.hash = hash;
            this.row = row;
        }

        public override int GetHashCode()
        {
            return hash;
        }


    }
}
