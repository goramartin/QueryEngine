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
    /// 
    /// The expression hasher contain reference to the ExpressionEqualityComparer class, that enables the hashers
    /// store the results of the expression computation into their internal variables. A simple cache so to speak.
    /// Because when the hash is computed, and the item is inserted into the dictionary. If the hashes are the same
    /// the two items will be compared. But the expressions of the inserting item has been computed already.
    /// So instead of cumputing it again, the information about the computation is stored into the equality comparer
    /// internal variables.
    /// 
    /// The clone method expects, that the cache list is different than the caches inside the (this).clone().
    /// In order to benefit from the cache.
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
                    hash = (((hash << 5) + hash) ^ this.hashers[i].Hash(in row));
                return hash;
            }
        }

        /// <summary>
        /// Creates a shallow copy of the row hasher.
        /// Note that the cache.size and the hashers.size are always the same.
        /// </summary>
        public RowHasher Clone(List<ExpressionEqualityComparer> cache)
        {
            var tmp = new List<ExpressionHasher>();
            for (int i = 0; i < cache.Count; i++)
                tmp.Add(this.hashers[i].Clone(cache[i]));

            return new RowHasher(tmp);
        }

        public void SetCache(List<ExpressionEqualityComparer> caches)
        {
            for (int i = 0; i < this.hashers.Count; i++)
                this.hashers[i].SetCache(caches[i]);
        }

        public void UnsetCache()
        {
            for (int i = 0; i < this.hashers.Count; i++)
                this.hashers[i].SetCache(null);
        }
    }
}
