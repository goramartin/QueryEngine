using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Struct serves as a type for the key that is put into the dictionary when multi
    /// group grouping is performed.
    /// </summary>
    internal readonly struct GroupDictKey
    {
        public readonly int hash;
        /// <summary>
        /// Position of the result row equivalent to the group representant.
        /// </summary>
        public readonly int position;

        public GroupDictKey(int hash, int position)
        {
            this.hash = hash;
            this.position = position;
        }


        public override int GetHashCode()
        {
            return hash;
        }

    }
}
