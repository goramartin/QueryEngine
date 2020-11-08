/*! \file
This file contains definition of a count function.
The count function can be specialised only on "number" type.

The count function have multiple uses.
1. count(*) - the function counts each row as a valid input.
2. count(x.PropName) - the function needs to evaluate the 
    value of x.PropName first and check if it is null or not.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueryEngine
{
    class BucketCount : AggregateBucket<int>
    {
        public BucketCount(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder == null) this.IsAstCount = true;
        }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (!this.IsAstCount)
            {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                    ((AggregateBucketResult<int>)bucket).aggResult++;
            }
            else ((AggregateBucketResult<int>)bucket).aggResult++;
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (!this.IsAstCount)
            {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                    Interlocked.Increment(ref ((AggregateBucketResult<int>)bucket).aggResult);
            }
            else Interlocked.Increment(ref ((AggregateBucketResult<int>)bucket).aggResult);
        }

        public override string ToString()
        {
            if (this.IsAstCount) return "Count(*)";
            else return "Count(" + this.expressionHolder.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "count";
        }

    }
}
