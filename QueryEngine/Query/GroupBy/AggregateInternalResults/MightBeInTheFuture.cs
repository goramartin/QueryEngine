using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine 
{
    /*
    internal abstract class AggregateResultsTest
    {
        public abstract void DoubleSize(int position);
        public abstract int Size { get; }
        public abstract bool IsSet(int position);
    }

    internal abstract class AggregateResultsTest<T>: AggregateResultsTest
    {
        public abstract T Get(int position);
        public abstract void Add(T value, int position);
        public abstract void AddThreadSafe(T value, int position);
        public abstract void AddEltsUsed(int value, int position);
        public abstract void AddEltsUsedThreadSafe(int value, int position);
        public abstract void Set(T value, int position);
        public abstract bool TrySet(T value, T initial, int position);
    }


#region List
    internal class AggregateListResultsTest<T> : AggregateResultsTest<T>
    {
        public List<T> aggResults = new List<T>();
        
        public override T Get(int position) => this.aggResults[position];
        public override void Set(T value, int position)
        {
            if (this.aggResults.Count == position) this.aggResults.Add(value);
            else this.aggResults[position] = value;
        }

        public override bool IsSet(int position) => throw new NotImplementedException();
        public override int Size => throw new NotImplementedException();
        public override void DoubleSize(int position) => throw new NotImplementedException();
        public override bool TrySet(T value, T initial, int position) => throw new NotImplementedException();
        public override void AddEltsUsed(int value, int position) => throw new NotImplementedException();
        public override void AddEltsUsedThreadSafe(int value, int position) => throw new NotImplementedException();
        public override void AddThreadSafe(T value, int position) => throw new NotImplementedException();
        public override void Add(T value, int position) => throw new NotImplementedException();
    }

    internal class AggregateListIntResultsTest : AggregateListResultsTest<int>
    {
        public override void Add(int value, int position) 
        {
            if (this.aggResults.Count == position) this.aggResults.Add(value);
            else this.aggResults[position] += value;
        }
    }

    internal class AggregateListAvgResultsTest : AggregateListIntResultsTest
    {
        public List<int> eltUsed = new List<int>();

        public override void AddEltsUsed(int value, int position)
        {
            if (this.eltUsed.Count == position) this.eltUsed.Add(value);
            else this.eltUsed[position] += value;
        }
    }

    #endregion List

    #region Bucket
    internal abstract class Bucket<T> : AggregateResultsTest<T>
    {
        public T aggResult = default;

        public override int Size => 1;
        public override T Get(int position) => this.aggResult;
        public override void DoubleSize(int position) => throw new NotImplementedException();
        public override void AddEltsUsed(int value, int position) => throw new NotImplementedException();
        public override void AddEltsUsedThreadSafe(int value, int position) => throw new NotImplementedException();
        public override bool IsSet(int position) => throw new NotImplementedException();
        public override void Set(T value, int position) => throw new NotImplementedException();
        public override bool TrySet(T value, T initial, int position) => throw new NotImplementedException();
        public override void Add(T value, int position) => throw new NotImplementedException();
        public override void AddThreadSafe(T value, int position) => throw new NotImplementedException();
    }

    internal class BucketInt : Bucket<int>
    {
        public override void Add(int value, int position)
        {
            this.aggResult += value;
        }
        public override void AddThreadSafe(int value, int position)
        {
            Interlocked.Add(ref this.aggResult, value);
        }
    }

    internal class BucketIntSet : BucketInt
    {
        public bool isSet = false;
        public override bool IsSet(int position) => isSet;
        public override void Set(int value, int position)
        {
            this.isSet = true;
            this.aggResult = value;
        }
        public override bool TrySet(int value, int initial, int position)
        {
            return initial == Interlocked.CompareExchange(ref this.aggResult, value, initial);
        }
    }
    internal class BucketStringSet : Bucket<string>
    {
        public bool isSet = false;
        public override bool IsSet(int position) => isSet;
        public override void Set(string value, int position)
        {
            this.isSet = true;
            this.aggResult = value;
        }
        public override bool TrySet(string value, string initial, int position)
        {
            return initial == Interlocked.CompareExchange(ref this.aggResult, value, initial);
        }
    }
    internal class BucketIntAvg : BucketInt
    {
        public int eltsUsed = default;

        public override void AddEltsUsed(int value, int position)
        {
            this.eltsUsed += value;
        }
        public override void AddEltsUsedThreadSafe(int value, int position)
        {
            Interlocked.Add(ref this.eltsUsed, value);
        }

    }
    #endregion Bucket

    #region Array

    internal abstract class ArrayTest<T> : AggregateResultsTest<T>
    {
        public static int InitSize { get; private set; } = 512;
        public T[] aggResults = new T[InitSize];
        int size = InitSize;

        public override int Size => size;
        public override T Get(int position) => this.aggResults[position];
        public override void DoubleSize(int position)
        {
            Array.Resize<T>(ref this.aggResults, (position + (position % 2)) * 2);
            size = (position + (position % 2)) * 2;
        }

        public override void AddEltsUsed(int value, int position) => throw new NotImplementedException();
        public override void AddEltsUsedThreadSafe(int value, int position) => throw new NotImplementedException();
        public override bool IsSet(int position) => throw new NotImplementedException();
        public override void Set(T value, int position) => throw new NotImplementedException();
        public override bool TrySet(T value, T initial, int position) => throw new NotImplementedException();
        public override void Add(T value, int position) => throw new NotImplementedException();
        public override void AddThreadSafe(T value, int position) => throw new NotImplementedException();
    }

    internal class ArrayInt : ArrayTest<int>
    {
        public override void Add(int value, int position)
        {
            this.aggResults[position] += value;
        }
        public override void AddThreadSafe(int value, int position)
        {
            Interlocked.Add(ref this.aggResults[position], value);
        }
    }

    internal class ArrayIntSet : ArrayInt
    {
        public bool[] isSet = new bool[ArrayTest<int>.InitSize];
        public override bool IsSet(int position) => isSet[position];
        public override void Set(int value, int position)
        {
            this.isSet[position] = true;
            this.aggResults[position] = value;
        }
        public override bool TrySet(int value, int initial, int position)
        {
            return initial == Interlocked.CompareExchange(ref this.aggResults[position], value, initial);
        }
    }
    internal class ArrayStringSet : ArrayTest<string>
    {
        public bool[] isSet = new bool[ArrayTest<string>.InitSize];
        public override bool IsSet(int position) => isSet[position];
        public override void Set(string value, int position)
        {
            this.isSet[position] = true;
            this.aggResults[position] = value;
        }
        public override bool TrySet(string value, string initial, int position)
        {
            return initial == Interlocked.CompareExchange(ref this.aggResults[position], value, initial);
        }
    }
    internal class ArrayIntAvg : ArrayInt
    {
        public int[] eltsUsed = new int[ArrayTest<int>.InitSize];

        public override void AddEltsUsed(int value, int position)
        {
            this.eltsUsed[position] += value;
        }
        public override void AddEltsUsedThreadSafe(int value, int position)
        {
            Interlocked.Add(ref this.eltsUsed[position], value);
        }
    }

    #endregion  Array

    */
}
