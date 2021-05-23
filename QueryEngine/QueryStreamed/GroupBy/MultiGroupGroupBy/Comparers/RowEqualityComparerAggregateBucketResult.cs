using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A class serves as EqualityComparer that is used during streamed group by where the key values are 
    /// stored directly in the buckets and not as row proxies.
    /// The first n buckets are used as keys inside the Dictionary, thus the first n values are compared for the key
    /// equality by calling static methods on the specialised buckets.
    /// </summary>
    internal class RowEqualityComparerAggregateBucketResult : IEqualityComparer<AggregateBucketResult[]>
    {
        /// <summary>
        /// A number of keys used to hash the passed array.
        /// The keys are expected to be located at the beginning of the array.
        /// </summary>
        private int keyCount;
        /// <summary>
        /// A type of first #keyCount buckets.
        /// Used for the jump table during comparison.
        /// </summary>
        private Type[] keyTypes;

        /// <summary>
        /// Constructs comparer.
        /// </summary>
        /// <param name="keyCount">A number of keys used to hash the passed array.</param>
        /// <param name="expressionHolders"> Expressions that define types of the keys.</param>
        public RowEqualityComparerAggregateBucketResult(int keyCount, ExpressionHolder[] expressionHolders)
        {
            this.keyCount = keyCount;
            this.keyTypes = new Type[expressionHolders.Length];
            for (int i = 0; i < expressionHolders.Length; i++)
                this.keyTypes[i] = expressionHolders[i].ExpressionType;
        }

        /// <summary>
        /// Compares only the first #keyCount buckets in the passed array.
        /// Because the buckets contain directly the value, there is no need for addtional classes
        /// used for comparison and also no need for Expression to compu the desired values.
        /// There is not that many types, so for now only a simple jump table is enough.
        /// </summary>
        public bool Equals(AggregateBucketResult[] x, AggregateBucketResult[] y)
        {
            for (int i = 0; i < this.keyCount; i++)
                if (!AggregateBucketResultStreamedComparers.Equals(keyTypes[i], x[i], y[i])) return false;

            return true;
        }

        /// <summary>
        /// Constructs a hash code from the first #keyCount buckets in the passed array.
        /// A djb2 hashing function using xor instead of + operation.
        /// Implementation taken from "http://www.cse.yorku.ca/~oz/hash.html" [last access 23.5.2021].
        /// Although it is formally a string hashing function, based on the "https://softwareengineering.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed" [last access 23.5.2021]
        /// we decided to use it as a general hasing function.
        /// </summary>
        public int GetHashCode(AggregateBucketResult[] obj)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < this.keyCount; i++)
                    hash = (((hash << 5) + hash) ^ obj[i].GetHashCode());
                return hash;
            }
        }
    }
}
