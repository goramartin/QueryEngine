using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Range of [Int32.MinValue, Int32.MaxValue].
    /// BucketCount = #thread * #thread .
    /// rangeSize = UInt32.MaxValue / BucketCount  .
    /// Notice that the content of the sticking bucket if (UInt32.MaxValue % BucketCount != 0) is fit into the last bucket.
    /// Because the thread count is restricted, it assumes that range size is always > 0.
    /// </summary>
    internal class IntRangeHasher : TypeRangeHasher<int>
    {
        private uint rangeSize;
        /// <summary>
        /// Represents values -2_147_483_648 (Int32.MinValue)
        /// </summary>
        private uint negativeRange = UInt32.MaxValue / 2 + 1;

        public IntRangeHasher(int threadCount) : base(threadCount)
        {
            this.BucketCount = threadCount * threadCount;
            this.rangeSize = (UInt32.MaxValue / (uint)this.BucketCount);
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

                var retVal = (int)(tmpValue / this.rangeSize);
                // Because the unassigned values (UInt32.MaxValue mod (uint)this.BucketCount) fall into the bucket after the last.
                // This can be done because the range is always larger than the unassigned values. So it always generates max +1 bucket.
                if (retVal == this.BucketCount) return retVal - 1;
                else return retVal;
            }
        }
    }



}
