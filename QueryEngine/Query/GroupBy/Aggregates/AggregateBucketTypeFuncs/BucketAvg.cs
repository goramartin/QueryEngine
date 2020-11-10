/*! \file
This file contains definition of a avg function.
The avg function can be specialised only on "number" type.
The class computes avg using incremental approach.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine 
{
    class IntBucketAvg : AggregateBucket<int>
    {
        public IntBucketAvg(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<int>)bucket);
                tmpBucket.aggResult += returnValue;
                tmpBucket.eltUsed++;
            }
        }

        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<int>)bucket);
                Interlocked.Add(ref tmpBucket.aggResult, returnValue);
                Interlocked.Increment(ref tmpBucket.eltUsed);
            }
        }
        public override void MergeTwoBuckets(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketAvgResult<int>)bucket1);
            var tmpBucket2 = ((AggregateBucketAvgResult<int>)bucket2);
            tmpBucket1.aggResult += tmpBucket2.aggResult;
            tmpBucket1.eltUsed += tmpBucket2.eltUsed;
        }

        public override void MergeTwoBucketsThreadSage(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketAvgResult<int>)bucket1);
            var tmpBucket2 = ((AggregateBucketAvgResult<int>)bucket2);
            Interlocked.Add(ref tmpBucket1.aggResult, tmpBucket2.aggResult);
            Interlocked.Add(ref tmpBucket1.eltUsed, tmpBucket2.eltUsed);
        }
        public override string ToString()
        {
            return "Avg(" + this.expressionHolder.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "avg";
        }
    }
}
