using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



    /// <summary>
    /// Range of Int32.MinValue ... Int32.MaxValue.
    /// BucketCount = #thread * #thread 
    /// rangeSize = UInt32.MaxValue / BucketCount  
    /// Notice that the content of the sticking bucket if (UInt32.MaxValue % BucketCount != 0) is fit into the last bucket.
    /// Because the thread count is restricted, it assumes that range size is always > 0;
    /// The
    /// </summary>
    internal class IntRangeHasher : TypeRangeHasher<int>
    {
        private uint rangeSize;
        /// <summary>
        /// Represents values - 2 147 483 648 (Int32.MinValue)
        /// </summary>
        private uint halfRange = UInt32.MaxValue / 2 + 1;
        

        /*
        /// <summary>
        ///  A point that devides the range into the buckets containing +1 range size from the rest.
        /// </summary>
        private uint distributionPoint;
        /// <summary>
        /// A number of buckets until the distribution point.
        /// </summary>
        private uint distributionBuckets;
         */

        public IntRangeHasher(int threadCount) : base(threadCount)
        {
            this.BucketCount = threadCount * threadCount;
            this.rangeSize = (UInt32.MaxValue / (uint)this.BucketCount);
            
            /*
            var unDistributedValues = (UInt32.MaxValue % (uint)this.BucketCount);
            this.distributionPoint = (rangeSize * (unDistributedValues)) + unDistributedValues;
            this.distributionBuckets = (unDistributedValues);
             */
        }

        public override int Hash(int value)
        {
            if (value == Int32.MinValue) return 0;
            else
            {
                uint tmpValue = 0;
                if (value < 0)
                    tmpValue = halfRange - ((uint)-value);
                else 
                    tmpValue = this.halfRange + (uint)value;

                return (int)(tmpValue / this.rangeSize);
                /*
                // Until distributionPoint, buckets have +1 on range.
                if (tmpValue <= distributionPoint)
                    return (int)(tmpValue / (this.rangeSize + 1));
                else 
                    return (int)((this.distributionBuckets) + (( tmpValue - distributionPoint) / this.rangeSize));
                 */
            }
        }
    }

    internal abstract class StringRangeHasher : TypeRangeHasher<string>
    {
        public StringRangeHasher(int threadCount) : base(threadCount)
        {}
    }

    /// <summary>
    /// A hasher that hashes into buckets based on the first two characters. 
    /// Assuming that they are Asii (0 - 127).
    /// It omits working the characters that are not printable (33 characters, codes 0 - 31 + 127).
    /// The entire range is 128 - 33 = 95, thus 95*95.
    /// The types of distribution are made into ints instead of uints unlike for int.
    /// </summary>
    internal class AsciiStringRangeHasher : StringRangeHasher
    {
        private int rangeSize;
        private int mainCharacterCount = 95;
        private int spaceChar = 32;
        private int delChar = 127;


        public AsciiStringRangeHasher(int threadCount) : base(threadCount)
        {
            this.BucketCount = threadCount * threadCount;
            this.rangeSize = (mainCharacterCount * mainCharacterCount) / this.BucketCount;
        }

        /// <summary>
        /// Note that this method is called only when the value is non empty and never null.
        /// </summary>
        public override int Hash(string value)
        {
            int firstChar = value[0];
            int secondChar = (value.Length >= 2 ? value[1] : 0);

            // Less then SPACE and not DEL

            // If non printable, choose the first bucket otherwise the DEL char is in last bucket.
            if (firstChar < spaceChar) return 0;
            else if (firstChar == delChar) return this.BucketCount - 1;
            else
            {
                // Non printable goes into the bucket where is the beginning of the letter as the first char.
                if (secondChar < spaceChar) secondChar = 0;
                // Otherwise it goes into the bucket where is the end of the letter as the first char.
                else if (secondChar == delChar) secondChar = this.mainCharacterCount - 1;
                
                // The range starts from 0.
                firstChar -= spaceChar;

                return ((firstChar * mainCharacterCount) + secondChar) / this.rangeSize;
            }
        }
    }


}
