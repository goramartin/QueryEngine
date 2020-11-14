using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a result holder for a group aggregated values during grouping.
    /// For every new group a new array filled with bucket result classes is created during grouping.
    /// Each bucket class encompases a result of the aggregation. 
    /// These storages will be used in parallel enviroment. 
    /// </summary>
    internal abstract class AggregateBucketResult
    {
        public static AggregateBucketResult[] CreateBucketResults(List<Aggregate> aggregates)
        {
            if (aggregates.Count == 0) return null;
            var aggResults = new AggregateBucketResult[aggregates.Count];
            for (int i = 0; i < aggResults.Length; i++)
                aggResults[i] = AggregateBucketResult.Factory(aggregates[i].GetAggregateReturnType(), aggregates[i].GetFuncName());

            return aggResults;
        }

        public static AggregateBucketResult Factory(Type type, string funcName)
        {
            if (type == typeof(int) && funcName == "avg") return new AggregateBucketAvgResult<int>();
            else if (type == typeof(int) && (funcName == "min" || funcName == "max")) return new AggregateBucketResultWithSetFlag<int>();
            else if (type == typeof(string) && (funcName == "min" || funcName == "max")) return new AggregateBucketResultWithSetFlag<string>();
            else if (type == typeof(int)) return new AggregateBucketResult<int>();
            else if (type == typeof(string)) return new AggregateBucketResult<string>();
            else throw new ArgumentException($"Aggregate bucket results factory, cannot create a results holder with the type {type} for function {funcName}.");
        }

    }

    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateBucketResult<T> : AggregateBucketResult
    {
        public T aggResult = default;
    }
    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateBucketAvgResult<T> : AggregateBucketResult<T>
    {
        public int eltUsed = 0;
    }
    /// <summary>
    /// Mainly its purpose is to initialise first values of the bucket, for example,
    /// a min/max aggregates must be initialised first otherwise they could compare new values with the
    /// default values from the constructor.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateBucketResultWithSetFlag<T> : AggregateBucketResult<T>
    {
        public bool isSet = false;
    }

}
