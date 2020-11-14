/*! \file
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
    internal class IntMax : Aggregate<int>
    {
        public IntMax(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }
        public override string ToString()
        {
            return "Max(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "max";
        }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<int>)bucket); ;
                if (tmpBucket.isSet)
                {
                    if (tmpBucket.aggResult < returnValue) tmpBucket.aggResult = returnValue;
                    else { /* nothing */ }
                }
                else
                {
                    tmpBucket.aggResult = returnValue;
                    tmpBucket.isSet = true;
                }
            }
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<int>)bucket);
                ApplyThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, returnValue);
            }
        }
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpList = (AggregateListResults<int>)list;
                if (position == tmpList.aggResults.Count) tmpList.aggResults.Add(returnValue);
                else if (tmpList.aggResults[position] < returnValue) tmpList.aggResults[position] = returnValue;
            }
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpArray = ((AggregateArrayResultsWithSetFlag<int>)array);
                ApplyThreadSafeInternal(ref tmpArray.aggResults[position], ref tmpArray.isSet[position], returnValue);
            }
        }

        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<int>)bucket1);
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<int>)bucket2);
            if (tmpBucket1.aggResult < tmpBucket2.aggResult) tmpBucket1.aggResult = tmpBucket2.aggResult;
            else { /* nothing */ }
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<int>)bucket1);
            // The second is not accessed anymore, because the group in dictionary represents
            // the first one.
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<int>)bucket2);

            // Compare-exchange mechanism.
            int initialValue, greaterValue;
            do
            {
                initialValue = tmpBucket1.aggResult;
                if (initialValue < tmpBucket2.aggResult) greaterValue = tmpBucket2.aggResult;
                else greaterValue = initialValue;
            }
            while (initialValue != Interlocked.CompareExchange(ref tmpBucket1.aggResult, greaterValue, initialValue));
        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<int>)list1;
            var tmpList2 = (AggregateListResults<int>)list2;

            if (into == tmpList1.aggResults.Count) tmpList1.aggResults.Add(tmpList2.aggResults[from]);
            else if (tmpList1.aggResults[into] < tmpList2.aggResults[from]) tmpList1.aggResults[into] = tmpList2.aggResults[from];
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResult<int>)bucket);
            var tmpList = ((AggregateListResults<int>)list);
            int initialValue, greaterValue;
            do
            {
                initialValue = tmpBucket.aggResult;
                if (initialValue.CompareTo(tmpList.aggResults[position]) < 0) greaterValue = tmpList.aggResults[position];
                else greaterValue = initialValue;
            }
            while (initialValue != Interlocked.CompareExchange(ref tmpBucket.aggResult, greaterValue, initialValue));
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResult<int>)bucket);
            var tmpList = ((AggregateListResults<int>)list);
            if (tmpBucket.aggResult < tmpList.aggResults[position]) tmpBucket.aggResult = tmpList.aggResults[position];
        }

        private void ApplyThreadSafeInternal(ref int value, ref bool isSet, int applied)
        {
            bool wasSet = false;
            while (!wasSet)
            {
                if (isSet)
                {
                    // Compare-exchange mechanism.
                    int initialValue, greaterValue;
                    do
                    {
                        initialValue = value;
                        if (initialValue < applied) greaterValue = applied;
                        else greaterValue = initialValue;
                    }
                    while (initialValue != Interlocked.CompareExchange(ref value, greaterValue, initialValue));
                    wasSet = true;
                }
                else
                {
                    // Note that this branch happens only when initing the first value.
                    lock (this)
                    {   // Check if other thread inited the first value while waiting.
                        if (!isSet)
                        {
                            // The sets must be in this order, because after setting IsSet flag
                            // there must be placed the value, otherwise thread could access empty bucket.
                            value = applied;
                            isSet = true;
                            wasSet = true;
                        }
                        else { /* next cycle it will go in the other branch of if(IsSet) */}
                    }
                }
            }
        }
    }

    internal class StrMax : Aggregate<string>
    {
        public StrMax(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }
        public override string ToString()
        {
            return "Max(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "max";
        }

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<string>)bucket);
                if (tmpBucket.isSet)
                {
                    if (tmpBucket.aggResult.CompareTo(returnValue) < 0) tmpBucket.aggResult = returnValue;
                    else { /* nothing */ }
                }
                else
                {
                    tmpBucket.aggResult = returnValue;
                    tmpBucket.isSet = true;
                }
            }
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<string>)bucket);
                ApplyThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, returnValue);
            }
        }
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpList = (AggregateListResults<string>)list;
                if (position == tmpList.aggResults.Count) tmpList.aggResults.Add(returnValue);
                else if (tmpList.aggResults[position].CompareTo(returnValue) < 0) tmpList.aggResults[position] = returnValue;
            }
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                var tmpArray = ((AggregateArrayResultsWithSetFlag<string>)array);
                ApplyThreadSafeInternal(ref tmpArray.aggResults[position], ref tmpArray.isSet[position], returnValue);
            }
        }

        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<string>)bucket1);
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<string>)bucket2);
            if (tmpBucket1.aggResult.CompareTo(tmpBucket2.aggResult) < 0) tmpBucket1.aggResult = tmpBucket2.aggResult;
            else { /* nothing */ }
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<string>)bucket1);
            // The second is not accessed anymore, because the group in dictionary represents
            // the first one.
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<string>)bucket2);

            // Compare-exchange mechanism.
            string initialValue, greaterValue;
            do
            {
                initialValue = tmpBucket1.aggResult;
                if (initialValue.CompareTo(tmpBucket2.aggResult) < 0) greaterValue = tmpBucket2.aggResult;
                else greaterValue = initialValue;
            }
            while (initialValue != Interlocked.CompareExchange(ref tmpBucket1.aggResult, greaterValue, initialValue));

        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<string>)list1;
            var tmpList2 = (AggregateListResults<string>)list2;

            if (into == tmpList1.aggResults.Count) tmpList1.aggResults.Add(tmpList2.aggResults[from]);
            else if (tmpList1.aggResults[into].CompareTo(tmpList2.aggResults[from]) < 0) tmpList1.aggResults[into] = tmpList2.aggResults[from];
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketAvgResult<string>)bucket);
            var tmpList = ((AggregateListAvgResults<string>)list);

            string initialValue, greaterValue;
            do
            {
                initialValue = tmpBucket.aggResult;
                if (initialValue.CompareTo(tmpList.aggResults[position]) < 0) greaterValue = tmpList.aggResults[position];
                else greaterValue = initialValue;
            }
            while (initialValue != Interlocked.CompareExchange(ref tmpBucket.aggResult, greaterValue, initialValue));
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResult<string>)bucket);
            var tmpList = ((AggregateListResults<string>)list);
            if (tmpBucket.aggResult.CompareTo(tmpList.aggResults[position]) < 0) tmpBucket.aggResult = tmpList.aggResults[position];
            
        }

        private void ApplyThreadSafeInternal(ref string value, ref bool isSet, string applied)
        {
            bool wasSet = false;
            while (!wasSet)
            {
                if (isSet)
                {
                    // Compare-exchange mechanism.
                    string initialValue, greaterValue;
                    do
                    {
                        initialValue = value;
                        if (initialValue.CompareTo(applied) < 0) greaterValue = applied;
                        else greaterValue = initialValue;
                    }
                    while (initialValue != Interlocked.CompareExchange(ref value, greaterValue, initialValue));
                    wasSet = true;
                }
                else
                {
                    // Note that this branch happens only when initing the first value.
                    lock (this)
                    {   // Check if other thread inited the first value while waiting.
                        if (!isSet)
                        {
                            // The sets must be in this order, because after setting IsSet flag
                            // there must be placed the value, otherwise thread could access empty bucket.
                            value = applied;
                            isSet = true;
                            wasSet = true;
                        }
                        else { /* next cycle it will go in the other branch of if(IsSet) */}
                    }
                }
            }
        }
    }
}
