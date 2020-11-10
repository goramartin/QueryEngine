﻿using System;
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
    /// </summary>
    internal abstract class AggregateArrayResults
    {
        public static List<AggregateArrayResults> CreateArrayResults(List<AggregateArray> aggregates)
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

    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateArrayResults<T> : AggregateArrayResults
    {
        public List<T> values = new List<T>();
    }

    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateArrayAvgResults<T> : AggregateArrayResults<T>
    {
        public List<int> eltUsed = new List<int>();
    }
}