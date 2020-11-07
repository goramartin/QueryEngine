using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
    internal abstract class AggregateArrayResults
    {
        public static List<AggregateArrayResults> CreateArrayResults(List<Aggregate> aggregates)
        {
            List<AggregateArrayResults> aggResults = new List<AggregateArrayResults>();
            for (int i = 0; i < aggregates.Count; i++)
                aggResults.Add(AggregateArrayResults.Factory(aggregates[i].GetAggregateReturnType(), aggregates[i].GetFuncName()));

            return aggResults;
        }

        public static AggregateArrayResults Factory(Type type, string funcName)
        {
            if (type == typeof(int) && funcName == "avg") return new AggregateArrayAvgResults<int>();
            else if (type == typeof(int)) return new AggregateArrayResults<int>();
            else if (type == typeof(string)) return new AggregateArrayResults<string>();
            else throw new ArgumentException($"Aggregate array results factory, cannot create a results holder with the type {type} for function {funcName}.");
        }
    }

    internal  class AggregateArrayResults<T> : AggregateArrayResults
    {
        public List<T> values;
    }

    internal class AggregateArrayAvgResults<T> : AggregateArrayResults<T>
    {
        public List<T> eltUsed;
    }
}
