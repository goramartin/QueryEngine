namespace QueryEngine
{
   
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
    internal class RowHasher : IExpressionHasher
    {
        public ExpressionHasher[] Hashers { get; private set; }

        public RowHasher(ExpressionHasher[] hashers)
        {
            this.Hashers = hashers;
        }

        public int Hash(in TableResults.RowProxy row)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < this.Hashers.Length; i++)
                    hash = (((hash << 5) + hash) ^ this.Hashers[i].Hash(in row));
                return hash;
            }
        }

        /// <summary>
        /// Creates a shallow copy of the row hasher.
        /// </summary>
        public RowHasher Clone()
        {
            var tmp = new ExpressionHasher[this.Hashers.Length];
            for (int i = 0; i < this.Hashers.Length; i++)
                tmp[i] = (this.Hashers[i].Clone());

            return new RowHasher(tmp);
        }

        public void SetCache(ExpressionEqualityComparer[] caches)
        {
            for (int i = 0; i < this.Hashers.Length; i++)
                this.Hashers[i].SetCache(caches[i]);
        }

        public void UnsetCache()
        {
            for (int i = 0; i < this.Hashers.Length; i++)
                this.Hashers[i].SetCache(null);
        }
    }
}
