using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A class that contains information about ranges of the first key in the streamed order by.
    /// For a given value it returns an index of the range the value belongs to.
    /// Each type should specify its own properties.
    /// The number of buckets and the size of the ranges depends on the number of threads that will access the buckets.
    /// It is expected that the number of threads accessing the buckets is restricted in some sensible way. For now lets choose 32.
    /// </summary>
    /// <typeparam name="T"> A type of the universum. </typeparam>
    internal abstract class TypeRangeHasher<T>
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
            // Values that are not distribued.
            var unDistributedValues = (UInt32.MaxValue % (uint)this.BucketCount);
            // A point that devides the range into the buckets containing +1 range size from the rest.
            this.distributionPoint = (rangeSize * (unDistributedValues)) + unDistributedValues;
            // A num
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

                return (int)(tmpValue / (this.rangeSize));

                /*
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

    internal class AsciiStringRangeHasher : StringRangeHasher
    {





        public override int Hash(string value)
        {
           
        }
    }


}
