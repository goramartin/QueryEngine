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
    /// The class is used solely for keys.
    /// </summary>
    internal class AggregateBucketResultStreamed<T> : AggregateBucketResult<T>
    {
        public bool isSet = false;
        public override int GetHashCode()
        {
            return this.aggResult.GetHashCode();
        }

        public bool GetValue(out T returnVal)
        {
            if (this.isSet) returnVal = this.aggResult;
            else returnVal = default;

            return this.isSet;
        }
    }

    internal static class AggregateBucketResultStreamedGetValue
    {
        public static T GetFinalValue<T>(AggregateBucketResult bucket)
        {
            return ((IGetFinal<T>)bucket).GetFinal(0);
        }
    }

    internal static class AggregateBucketResultStreamedComparers
    {
        /// <summary>
        /// A methods that is used only in the context of fully streamed version of group by.
        /// The keys put in the concurrent dictionary will be of the base type AggregateBucketResult<T> since
        /// the keys and agg value will be stored in the same array to save a bit of memory.
        /// Note that the values must always be set beforehand.
        /// </summary>
        public static bool Compare(Type type, AggregateBucketResult x, AggregateBucketResult y)
        {
            if (type == typeof(int))
                return Compare((AggregateBucketResultStreamed<int>)x, (AggregateBucketResultStreamed<int>)y);
            else if ((type == typeof(string)))
                return Compare((AggregateBucketResultStreamed<string>)x, (AggregateBucketResultStreamed<string>)y);
            else throw new ArgumentException($"Aggregate bucket result compare, unkown type to compare. Type = {type}.");
        }

       public static bool Compare(AggregateBucketResultStreamed<int> x, AggregateBucketResultStreamed<int> y)
       {
            if (x.isSet && y.isSet) return x.aggResult == y.aggResult;
            else if ((!x.isSet && y.isSet) || (x.isSet && !y.isSet)) return false;
            else return true;
       }
        public static bool Compare(AggregateBucketResultStreamed<string> x, AggregateBucketResultStreamed<string> y)
        {
            if (x.isSet && y.isSet) return x.aggResult == y.aggResult;
            else if ((!x.isSet && y.isSet) || (x.isSet && !y.isSet)) return false;
            else return true;
        }
    }
}
