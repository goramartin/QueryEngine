using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
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
        private uint negativeRange = UInt32.MaxValue / 2 + 1;


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
                    tmpValue = this.negativeRange - (uint)(-value);
                else
                    tmpValue = this.negativeRange + (uint)value;

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


}
