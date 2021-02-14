using System;

namespace QueryEngine
{
    internal class TypeRangeHasher
    {
        public static TypeRangeHasher Factory(int threadCount, Type type)
        {
            if (type == typeof(int)) return new IntRangeHasher(threadCount);
            else if (type == typeof(string)) return new AsciiStringRangeHasher(threadCount);
            else throw new ArgumentException($"Type range hasher factory, trying to create unknown type. Type == {type}");
        }
    }

    /// <summary>
    /// A class that contains information about ranges of the first key in the streamed order by.
    /// For a given value it returns an index of the range the value belongs to.
    /// Each type should specify its own properties.
    /// The number of buckets and the size of the ranges depends on the number of threads that will access the buckets.
    /// It is expected that the number of threads accessing the buckets is restricted in some sensible way. For now lets choose 32.
    /// </summary>
    /// <typeparam name="T"> A type of the universum. </typeparam>
    internal abstract class TypeRangeHasher<T> : TypeRangeHasher
    {
        protected int threadCount;
        public int BucketCount { get; protected set; }

        public abstract int Hash(T value);

        protected TypeRangeHasher(int threadCount) 
        {
            if (threadCount > 32)
                throw new ArgumentException($"{this.GetType()}, exceeded the maximum number of threads.");
            this.threadCount = threadCount;
        }
    }

}
