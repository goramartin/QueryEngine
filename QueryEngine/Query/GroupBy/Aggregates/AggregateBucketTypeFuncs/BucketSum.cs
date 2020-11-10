/*! \file
This file contains definition of a sum function.
The sum function can be specialised only on "number" types.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class IntBucketSum : AggregateBucket<int>
    {
        public IntBucketSum(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                ((AggregateBucketResult<int>)bucket).aggResult += returnValue;
        }

        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                Interlocked.Add(ref ((AggregateBucketResult<int>)bucket).aggResult, returnValue);
        }

        public override void MergeTwoBuckets(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            ((AggregateBucketResult<int>)bucket1).aggResult += ((AggregateBucketResult<int>)bucket2).aggResult;
        }

        public override void MergeTwoBucketsThreadSage(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            Interlocked.Add(ref ((AggregateBucketResult<int>)bucket1).aggResult, ((AggregateBucketResult<int>)bucket2).aggResult);
        }

        public override string ToString()
        {
            return "Sum(" + this.expressionHolder.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "sum";
        }

    }
}
