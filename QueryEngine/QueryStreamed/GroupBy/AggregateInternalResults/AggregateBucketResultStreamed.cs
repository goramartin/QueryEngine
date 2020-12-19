using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A class that will represent a key part in the dictionary for streamed version.
    /// The key/value will be included in one array instead of two separate ones. Thus, it can
    /// save a bit of memory.
    /// </summary>
    internal class AggregateBucketResultStreamed<T> : AggregateBucketResult<T>
    {
        public bool isSet = false;
        public override int GetHashCode()
        {
            return this.aggResult.GetHashCode();
        }
    }
}
