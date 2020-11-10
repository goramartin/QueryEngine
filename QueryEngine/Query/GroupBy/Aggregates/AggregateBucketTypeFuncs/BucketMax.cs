﻿/*! \file
This file contains definition of a max function.
The max function can be specialised on any type.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine
{
    internal class IntBucketMax : AggregateBucket<int>
    {
        public IntBucketMax(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<int>)bucket); ;
                if (tmpBucket.IsSet)
                {
                    if (tmpBucket.aggResult < returnValue) tmpBucket.aggResult = returnValue;
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
                        int initialValue, greaterValue;
                        do
                        {
                            initialValue = tmpBucket.aggResult;
                            if (initialValue < returnValue) greaterValue = returnValue;
                            else greaterValue = initialValue;
                        }
                        while (initialValue != Interlocked.CompareExchange(ref tmpBucket.aggResult, greaterValue, initialValue));
                        wasSet = true;
                    }
                    else
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
                            } else { /* next cycle it will go in the other branch of if(IsSet) */}
                        }
                    }
                }
            }
        }

        public override void MergeTwoBuckets(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<int>)bucket1);
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<int>)bucket2);
            if (tmpBucket1.aggResult > tmpBucket2.aggResult) tmpBucket1.aggResult = tmpBucket2.aggResult;
            else { /* nothing */ }
        }

        public override void MergeTwoBucketsThreadSage(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<int>)bucket1);
            // The second is not accessed anymore, because the group in dictionary represents
            // the first one.
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<int>)bucket2);

            // Compare-exchange mechanism.
            int initialValue, smallerValue;
            do
            {
                initialValue = tmpBucket1.aggResult;
                if (initialValue > tmpBucket2.aggResult) smallerValue = tmpBucket2.aggResult;
                else smallerValue = initialValue;
            }
            while (initialValue != Interlocked.CompareExchange(ref tmpBucket1.aggResult, smallerValue, initialValue));
        }

        public override string ToString()
        {
            return "Max(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "max";
        }
    }

    internal class StrBucketMax : AggregateBucket<string>
    {
        public StrBucketMax(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<string>)bucket);
                if (tmpBucket.IsSet)
                {
                    if (tmpBucket.aggResult.CompareTo(returnValue) < 0) tmpBucket.aggResult = returnValue;
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
                        // Compare-echange mechanism.
                        string initialValue, greaterValue;
                        do
                        {
                            initialValue = tmpBucket.aggResult;
                            if (initialValue.CompareTo(returnValue) < 0) greaterValue = returnValue;
                            else greaterValue = initialValue;
                        }
                        while (initialValue != Interlocked.CompareExchange(ref tmpBucket.aggResult, greaterValue, initialValue));
                        wasSet = true;
                    }
                    else
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

        public override void MergeTwoBuckets(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<string>)bucket1);
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<string>)bucket2);
            if (tmpBucket1.aggResult.CompareTo(tmpBucket2.aggResult) < 0) tmpBucket1.aggResult = tmpBucket2.aggResult;
            else { /* nothing */ }
        }

        public override void MergeTwoBucketsThreadSage(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<string>)bucket1);
            // The second is not accessed anymore, because the group in dictionary represents
            // the first one.
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<string>)bucket2);

            // Compare-exchange mechanism.
            string initialValue, smallerValue;
            do
            {
                initialValue = tmpBucket1.aggResult;
                if (initialValue.CompareTo(tmpBucket2.aggResult) < 0) smallerValue = tmpBucket2.aggResult;
                else smallerValue = initialValue;
            }
            while (initialValue != Interlocked.CompareExchange(ref tmpBucket1.aggResult, smallerValue, initialValue));

        }
        public override string ToString()
        {
            return "Max(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "max";
        }
    }
}
