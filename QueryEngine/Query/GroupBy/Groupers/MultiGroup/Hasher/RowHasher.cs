using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal interface IRowHasher
    {
        int Hash(in TableResults.RowProxy row);
    }

    /// <summary>
    /// Creates a hash for a given row.
    /// For null values (missing property on an element) the returned hash is 0.
    /// The final hash is computed from all the hash expressions (defined in the group by clause.)
    /// </summary>
    internal class RowHasher : IRowHasher
    {
        protected List<ExpressionHasher> hashers;

        public RowHasher(List<ExpressionHasher> hashers)
        {
            this.hashers = hashers;
        }

        public int Hash(in TableResults.RowProxy row)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < this.hashers.Count; i++)
                    hash = 33 * hash + this.hashers[i].Hash(in row);
                return hash;
            }
        }

        /// <summary>
        /// Creates a hard copy of the row hasher.
        /// </summary>
        public RowHasher Clone()
        {
            var tmp = new List<ExpressionHasher>();
            for (int i = 0; i < 0; i++)
                tmp.Add(this.hashers[i].Clone());

            return new RowHasher(tmp);
        }
    }
}
