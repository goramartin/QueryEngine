using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a result holder for a group aggregated values during grouping.
    /// For every new group a new array filled with bucket result classes is created during grouping.
    /// Each bucket class encompases a result of the aggregation. 
    /// </summary>
    internal class AggregateBucketResult
    {
        public static AggregateBucketResult[] CreateBucketResults(List<Aggregate> aggregates)
        {
            var aggResults = new AggregateBucketResult[aggregates.Count];
            for (int i = 0; i < aggResults.Length; i++)
                aggResults[i] = AggregateBucketResult.Factory(aggregates[i].GetAggregateReturnType(), aggregates[i].GetFuncName());

            return aggResults;
        }

        public static AggregateBucketResult Factory(Type type, string funcName)
        {
            if (type == typeof(int) && funcName == "avg") return new AggregateBucketResult<int>();
            else if (type == typeof(int)) return new AggregateBucketResult<int>();
            else if (type == typeof(string)) return new AggregateBucketResult<string>();
            else throw new ArgumentException($"Aggregate bucket results factory, cannot create a results holder with the type {type} for function {funcName}.");
        }
    }

    internal class AggregateBucketResult<T> : AggregateBucketResult
    {
        public T aggResult = default;
    }
    internal class AggregateBucketAvgResult<T> : AggregateBucketResult<T>
    {
        public int eltUsed = 0;
    }
    internal class AggregateBucketResultWithSetFlag<T> : AggregateBucketResult<T>
    {
        public bool IsSet = false;
    }

}
