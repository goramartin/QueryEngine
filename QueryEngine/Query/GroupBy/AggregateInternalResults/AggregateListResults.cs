﻿using System;
using System.Collections.Generic;

namespace QueryEngine 
{
    /// <summary>
    /// A class represents a result holder for a group aggregated values during grouping.
    /// For each aggregate, a holder containing a List of value. The List is a holder for the aggregate 
    /// value for each individual group (index represents a group and its aggregate value).
    /// This approach can save a lot of memory because it does not have to allocate additional classes for every
    /// new group.
    /// These storages will not be used in the parallel enviroment. (Only in the local sense.)
    /// They are used only in the local group local merge algorithm, because there are no atomic operations
    /// on Lists. Thus instead of Lists,
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
            if (type == typeof(int) && funcName == "avg") return new AggregateListAvgLongResults();
            else if (type == typeof(int) && (funcName == "min" || funcName == "max")) return new AggregateListResultsWithSetFlag<int>();
            else if (type == typeof(string) && (funcName == "min" || funcName == "max")) return new AggregateListResultsWithSetFlag<string>();
            else if (funcName == "count") return new AggregateListResults<int>();
            else if (funcName == "sum") return new AggregateListResults<long>();
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

    internal class AggregateListResultsWithSetFlag<T>: AggregateListResults<T>
    {
        public List<bool> isSet = new List<bool>();
    }

    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// The need for the parameter is that the results
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateListAvgResults<T> : AggregateListResults<T>
    {
        public List<int> resultCount = new List<int>();
    }

    /// <summary>
    /// This class declaration is important, because the final result from the average should return double.
    /// The classes that will work with the class, know in advance that the returning type is double, thus,
    /// they will never access the class via the IGetFinal with type of int.
    /// </summary>
    internal sealed class AggregateListAvgLongResults : AggregateListAvgResults<long>, IGetFinal<double>
    {
        double IGetFinal<double>.GetFinal(int position)
        {
            return (double)this.aggResults[position] / this.resultCount[position];
        }
    }

}
