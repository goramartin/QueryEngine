/*! \file
This file contains definition of a min function.
The min function can be specialised on any type.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.AccessControl;

namespace QueryEngine
{
    internal class IntBucketMin : AggregateBucket<int>
    {
        public IntBucketMin(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<int>)bucket);
                if (tmpBucket.IsSet)
                {
                    if (tmpBucket.aggResult > returnValue) tmpBucket.aggResult = returnValue;
                    else { /* nothing */ }
                }
                else
                {
                    tmpBucket.aggResult = returnValue;
                    tmpBucket.IsSet = true;
                }
            }
        }

        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<int>)bucket);
                bool wasSet = false;
                while (!wasSet)
                {
                    if (tmpBucket.IsSet)
                    {
                        // Compare-exchange mechanism.
                        int initialValue, smallerValue;
                        do
                        {
                            initialValue = tmpBucket.aggResult;
                            if (initialValue > returnValue) smallerValue = returnValue;
                            else smallerValue = initialValue;
                        }
                        while (initialValue != Interlocked.CompareExchange(ref tmpBucket.aggResult, smallerValue, initialValue));
                        wasSet = true;
                    } else
                    {
                        // Note that this branch happens only when initing the first value.
                        lock (this)
                        {   // Check if other thread inited the first value while waiting.
                            if (!tmpBucket.IsSet)
                            {
                                // The sets must be in this order, because after setting IsSet flag
                                // there must be placed the value, otherwise thread could access empty bucket.
                                tmpBucket.aggResult = returnValue;
                                tmpBucket.IsSet = true;
                                wasSet = true;
                            }
                            else { /* next cycle it will go in the other branch of if(IsSet) */}
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Min(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "min";
        }
    }


    internal class StrBucketMin : AggregateBucket<string>
    {
        public StrBucketMin(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<string>)bucket);
                if (tmpBucket.IsSet)
                {
                    if (tmpBucket.aggResult.CompareTo(returnValue) > 0) tmpBucket.aggResult = returnValue;
                    else { /* nothing */ }
                }
                else
                {
                    tmpBucket.aggResult = returnValue;
                    tmpBucket.IsSet = true;
                }
            }
        }

        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<string>)bucket);
                bool wasSet = false;
                while (!wasSet)
                {
                    if (tmpBucket.IsSet)
                    {
                        // Compare exchange mechanism.
                        string initialValue, smallerValue;
                        do
                        {
                            initialValue = tmpBucket.aggResult;
                            if (initialValue.CompareTo(returnValue) > 0) smallerValue = returnValue;
                            else smallerValue = initialValue;
                        }
                        while (initialValue != Interlocked.CompareExchange(ref tmpBucket.aggResult, smallerValue, initialValue));
                        wasSet = true;
                    } else
                    {
                        // Note that this branch happens only when initing the first value.
                        lock (this)
                        {   // Check if other thread inited the first value while waiting.
                            if (!tmpBucket.IsSet)
                            {
                                // The sets must be in this order, because after setting IsSet flag
                                // there must be placed the value, otherwise thread could access empty bucket.
                                tmpBucket.aggResult = returnValue;
                                tmpBucket.IsSet = true;
                                wasSet = true;
                            }
                            else { /* next cycle it will go in the other branch of if(IsSet) */}
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Min(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "min";
        }

    }
}
