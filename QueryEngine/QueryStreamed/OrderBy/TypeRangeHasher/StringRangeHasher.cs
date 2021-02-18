using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal abstract class StringRangeHasher : TypeRangeHasher<string>
    {
        public StringRangeHasher(int threadCount) : base(threadCount)
        { }
    }

    /// <summary>
    /// A hasher that hashes into buckets based on the first two characters based on their ordinal values.
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

        /// <summary>
        ///  A point that devides the range into the buckets containing +1 range size from the rest.
        /// </summary>
        private int distributionPoint;
        /// <summary>
        /// A number of buckets until the distribution point.
        /// </summary>
        private int distributionBuckets;


        public AsciiStringRangeHasher(int threadCount) : base(threadCount)
        {
            this.BucketCount = threadCount * threadCount;
            this.rangeSize = (mainCharacterCount * mainCharacterCount) / this.BucketCount;

            var unDistributedValues = ((mainCharacterCount * mainCharacterCount) % this.BucketCount);
            this.distributionPoint = (rangeSize * (unDistributedValues)) + unDistributedValues;
            this.distributionBuckets = (unDistributedValues);
        }

        /// <summary>
        /// Note that this method is called only when the value is non empty and never null.
        /// </summary>
        public override int Hash(string value)
        {
            int firstChar = (value.Length > 0 ? value[0] : 0);
            int secondChar = (value.Length >= 2 ? value[1] : 0);

            // Less then SPACE and not DEL
            // If non printable, choose the first bucket otherwise the DEL char is in last bucket.
            if (firstChar < spaceChar) return 0;
            else if (firstChar == delChar) return this.BucketCount - 1;
            else
            {
                firstChar -= spaceChar;

                // Non printable goes into the bucket where is the beginning of the letter as the first char.
                if (secondChar < spaceChar) secondChar = 0;
                // Otherwise it goes into the bucket where is the end of the letter as the first char.
                else if (secondChar == delChar) secondChar = this.mainCharacterCount - 1;
                else secondChar -= spaceChar;

                var stringPos = ((firstChar * mainCharacterCount) + secondChar);
                // Until distributionPoint, buckets have +1 on range.
                if (stringPos <= distributionPoint) return (stringPos / (this.rangeSize + 1));
                else return ((this.distributionBuckets) + ((stringPos - distributionPoint) / this.rangeSize));

                /*
                // Never occurs.
                if (retVal == this.BucketCount) return retVal - 1;
                else return retVal;
                */
            }
        }
    }
}
