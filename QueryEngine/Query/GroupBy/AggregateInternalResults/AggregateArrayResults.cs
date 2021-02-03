using System;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a result holder for a group aggregated values during grouping.
    /// For each aggregate, a holder containing an array of values. The array is a holder for the aggregate 
    /// values for each individual group (index represents a group and its aggregate value).
    /// This approach can save a lot of memory because it does not have to allocate additional classes for every
    /// new group.
    /// The purpose of this alternative class using arrays is that there can be no atomic operations on lists.
    /// Thus during algorithms where there is multiple accesses into a list, the list cannot be used.
    /// Also, the arrays will provide method that will expand the array using the same way as the list.
    /// 
    /// Note the initial size. The main purpose of these classes is a global grouping. The initial size,
    /// can skip a lot of synchronization in the beginning of the grouping algorithm.
    /// </summary>
    internal abstract class AggregateArrayResults
    {
        // Always must be a > 0 and multiple of 2.
        public static int InitSize { get; private set; } = 512;

        public static AggregateArrayResults[] CreateArrayResults(Aggregate[] aggregates)
        {
            AggregateArrayResults[] aggResults = new AggregateArrayResults[aggregates.Length];
            for (int i = 0; i < aggregates.Length; i++)
                aggResults[i] = (AggregateArrayResults.Factory(aggregates[i].GetAggregateReturnType(), aggregates[i].GetFuncName()));

            return aggResults;
        }

        public static AggregateArrayResults Factory(Type type, string funcName)
        {
            if (type == typeof(int) && funcName == "avg") return new AggregateArrayAvgResults<int>();
            else if (type == typeof(int) && (funcName == "min" || funcName == "max")) return new AggregateArrayResultsWithSetFlag<int>();
            else if (type == typeof(string) && (funcName == "min" || funcName == "max")) return new AggregateArrayResultsWithSetFlag<string>();
            else if (type == typeof(int)) return new AggregateArrayResults<int>();
            else if (type == typeof(string)) return new AggregateArrayResults<string>();
            else throw new ArgumentException($"Aggregate bucket results factory, cannot create a results holder with the type {type} for function {funcName}.");
        }

        public abstract int ArraySize();
        public abstract void DoubleSize(int position);
    }

    /// <typeparam name="T"> A return type of the aggregation function. </typeparam>
    internal class AggregateArrayResults<T> : AggregateArrayResults, IGetFinal<T>
    {
        public T[] aggResults = new T[InitSize];
        public int size = InitSize;

        public override void DoubleSize(int position)
        {
            Array.Resize<T>(ref this.aggResults, (position + (position % 2)) * 2);
            size = (position + (position % 2)) * 2;
        }

        public override int ArraySize()
        {
            return this.size;
        }

        T IGetFinal<T>.GetFinal(int position)
        {
            return this.aggResults[position];
        }
    }

    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// </summary>
    internal class AggregateArrayAvgResults<T> : AggregateArrayResults<T>
    {
        public int[] eltsUsed = new int[InitSize];

        public override void DoubleSize(int position)
        {
            Array.Resize<int>(ref this.eltsUsed, (position + (position % 2)) * 2);
            base.DoubleSize( position);
        }
    }

    internal class AggregateArrayAvgResults : AggregateArrayAvgResults<int>, IGetFinal<double>
    {
        double IGetFinal<double>.GetFinal(int position)
        {
            return (double)this.aggResults[position] / this.eltsUsed[position];
        }
    }


    /// <summary>
    /// Mainly it is used during computing average.
    /// The class must rememeber the number of added elements to the computed average.
    /// </summary>
    internal class AggregateArrayResultsWithSetFlag<T> : AggregateArrayResults<T>
    {
        public bool[] isSet = new bool[InitSize];
        public override void DoubleSize(int position)
        {
            Array.Resize<bool>(ref this.isSet, (position + (position % 2)) * 2);
            base.DoubleSize(position);
        }
    }
}
