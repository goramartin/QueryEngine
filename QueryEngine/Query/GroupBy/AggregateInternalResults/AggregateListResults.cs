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
        public static AggregateListResults[] CreateListResults(Aggregate[] aggregates)
        {
            AggregateListResults[] aggResults = new AggregateListResults[aggregates.Length];
            for (int i = 0; i < aggregates.Length; i++)
                aggResults[i] = (AggregateListResults.Factory(aggregates[i].GetAggregateReturnType(), aggregates[i].GetFuncName()));

            return aggResults;
        }

        public static AggregateListResults Factory(Type type, string funcName)
        {
            if (type == typeof(int) && funcName == "avg") return new AggregateListAvgIntResults();
            else if (type == typeof(int)) return new AggregateListResults<int>();
            else if (type == typeof(string)) return new AggregateListResults<string>();
            else throw new ArgumentException($"Aggregate list results factory, cannot create a results holder with the type {type} for function {funcName}.");
        }
    }

    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateListResults<T> : AggregateListResults, IGetFinal<T>
    {
        public List<T> aggResults = new List<T>();

        T IGetFinal<T>.GetFinal(int position)
        {
            return this.aggResults[position];
        }
    }

    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// The need for the parameter is that the results
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateListAvgResults<T> : AggregateListResults<T>
    {
        public List<int> eltsUsed = new List<int>();
    }

    /// <summary>
    /// This class declaration is important, because the final result from the average should return double.
    /// The classes that will work with the class, know in advance that the returning type is double, thus,
    /// they will never access the class via the IGetFinal with type of int.
    /// </summary>
    internal sealed class AggregateListAvgIntResults : AggregateListAvgResults<int>, IGetFinal<double>
    {
        double IGetFinal<double>.GetFinal(int position)
        {
            return (double)this.aggResults[position] / this.eltsUsed[position];
        }
    }

}
