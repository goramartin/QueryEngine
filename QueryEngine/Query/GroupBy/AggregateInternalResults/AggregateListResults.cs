using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
    /// <summary>
    /// Class represents a result holder for a group aggregated values during grouping.
    /// For each aggregate, a holder containing a list of value. The list is a holder for the aggregate 
    /// value for each individual group (index represents a group and its aggregate value).
    /// This approach can save a lot of memory because it does not have to allocate additional classes for every
    /// new group.
    /// These storages will not be used in the parallel enviroment. (Only in the local sense.)
    /// They are used only in the local group local merge algorithm, because there are no atomic operations
    /// on lists. Thus instead of lists,
    /// </summary>
    internal abstract class AggregateListResults
    {
        public static List<AggregateListResults> CreateArrayResults(List<Aggregate> aggregates)
        {
            List<AggregateListResults> aggResults = new List<AggregateListResults>();
            for (int i = 0; i < aggregates.Count; i++)
                aggResults.Add(AggregateListResults.Factory(aggregates[i].GetAggregateReturnType(), aggregates[i].GetFuncName()));

            return aggResults;
        }

        public static AggregateListResults Factory(Type type, string funcName)
        {
            if (type == typeof(int) && funcName == "avg") return new AggregateListAvgResults<int>();
            else if (type == typeof(int)) return new AggregateListResults<int>();
            else if (type == typeof(string)) return new AggregateListResults<string>();
            else throw new ArgumentException($"Aggregate list results factory, cannot create a results holder with the type {type} for function {funcName}.");
        }
    }

    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateListResults<T> : AggregateListResults
    {
        public List<T> aggResults = new List<T>();
    }

    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateListAvgResults<T> : AggregateListResults<T>
    {
        public List<int> eltsUsed = new List<int>();
    }
}
