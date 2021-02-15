/*! \file
This file contains definition of a min and max function.
The min/max functions can be specialised on any type.
 */

using System.Threading;

namespace QueryEngine
{
    internal abstract class MinMaxBase<T> : Aggregate<T>
    {
        public MinMaxBase(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected abstract bool Compare(T x, T y);
        protected abstract bool CompareExchange(ref T value, T applied, T initial);


        // Buckets
        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
                if (tmpBucket.isSet)
                {
                    if (Compare(tmpBucket.aggResult, returnValue)) tmpBucket.aggResult = returnValue;
                    else { }
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
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
                ApplyThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, returnValue, tmpBucket);
            }
        }
        public override void Apply(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
                if (tmpBucket.isSet)
                {
                    if (Compare(tmpBucket.aggResult, returnValue)) tmpBucket.aggResult = returnValue;
                    else { }
                }
                else
                {
                    tmpBucket.aggResult = returnValue;
                    tmpBucket.isSet = true;
                }
            }
        }
        public override void ApplyThreadSafe(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
                ApplyThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, returnValue, tmpBucket);
            }
        }


        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<T>)bucket1);
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<T>)bucket2);
            if (Compare(tmpBucket1.aggResult, tmpBucket2.aggResult)) tmpBucket1.aggResult = tmpBucket2.aggResult;
            else { }
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<T>)bucket1);
            // The second is not accessed anymore, because the group in dictionary represents
            // the first one.
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<T>)bucket2);
            MergeThreadSafeInternal(ref tmpBucket1.aggResult, tmpBucket2.aggResult);
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResult<T>)bucket);
            var tmpList = ((AggregateListResults<T>)list);
            if (Compare(tmpBucket.aggResult, tmpList.aggResults[position])) tmpBucket.aggResult = tmpList.aggResults[position];
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResult<T>)bucket);
            var tmpList = ((AggregateListResults<T>)list);
            MergeThreadSafeInternal(ref tmpBucket.aggResult, tmpList.aggResults[position]);
        }
      

        // Lists
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpList = (AggregateListResults<T>)list;
                if (position == tmpList.aggResults.Count) tmpList.aggResults.Add(returnValue);
                else if (Compare(tmpList.aggResults[position], returnValue)) tmpList.aggResults[position] = returnValue;
            }
        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<T>)list1;
            var tmpList2 = (AggregateListResults<T>)list2;

            if (into == tmpList1.aggResults.Count) tmpList1.aggResults.Add(tmpList2.aggResults[from]);
            else if (Compare(tmpList1.aggResults[into], tmpList2.aggResults[from])) tmpList1.aggResults[into] = tmpList2.aggResults[from];
        }
        
        // Arrays
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpArray = ((AggregateArrayResultsWithSetFlag<T>)array);
                ApplyThreadSafeInternal(ref tmpArray.aggResults[position], ref tmpArray.isSet[position], returnValue, tmpArray);
            }
        }


        protected void MergeThreadSafeInternal(ref T value, T applied)
        {
            T initialValue, replacement;
            do
            {
                initialValue = value;
                if (Compare(initialValue, applied)) replacement = applied;
                // The values can grow either up (max) or down (min).
                // Because the comparison failed and in the meantime the value could be exchanged only for smaller (min) or greater (max).
                // Thus, the applied value can never succeed in the first place.
                else break; //replacement = initialValue;
            }
            while (!CompareExchange(ref value, replacement, initialValue));
        }
        protected void ApplyThreadSafeInternal(ref T value, ref bool isSet, T applied, object lockingObject)
        {
            bool wasSet = false;
            while (!wasSet)
            {
                if (isSet)
                {
                    MergeThreadSafeInternal(ref value, applied);
                    wasSet = true;
                }
                else
                {
                    // Note that this branch happens only when initing the first value.
                    lock (lockingObject)
                    {   // Check if other thread inited the first value while waiting.
                        if (!isSet)
                        {
                            // The sets must be in this order, because after setting IsSet flag
                            // there must be placed value, otherwise thread could access empty bucket.
                            value = applied;
                            isSet = true; 
                            wasSet = true;  
                        }
                        else { /* Next cycle -> it will go in the other branch of if(IsSet). */}
                    }
                }
            }
        }
    }

    



    internal abstract class Min<T> : MinMaxBase<T>
    {
        public Min(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override string ToString()
        {
            return "Min(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "min";
        }
    }
    internal class IntMin : Min<int>
    {
        public IntMin(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected override bool Compare(int x, int y)
        {
            return (x.CompareTo(y) > 0);
        }

        protected override bool CompareExchange(ref int value, int applied, int initial)
        {
            return (initial == Interlocked.CompareExchange(ref value, applied, initial));
        }
    }
    internal class StrMin : Min<string>
    {
        public StrMin(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected override bool Compare(string x, string y)
        {
            return (x.CompareTo(y) > 0);
        }

        protected override bool CompareExchange(ref string value, string applied, string initial)
        {
            return (initial == Interlocked.CompareExchange(ref value, applied, initial));
        }
    }

    internal abstract class Max<T> : MinMaxBase<T>
    {
        public Max(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override string ToString()
        {
            return "Max(" + this.expr.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "max";
        }
    }
    internal class IntMax : Min<int>
    {
        public IntMax(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected override bool Compare(int x, int y)
        {
            return (x.CompareTo(y) < 0);
        }

        protected override bool CompareExchange(ref int value, int applied, int initial)
        {
            return (initial == Interlocked.CompareExchange(ref value, applied, initial));
        }
    }
    internal class StrMax : Min<string>
    {
        public StrMax(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected override bool Compare(string x, string y)
        {
            return (x.CompareTo(y) < 0);
        }

        protected override bool CompareExchange(ref string value, string applied, string initial)
        {
            return (initial == Interlocked.CompareExchange(ref value, applied, initial));
        }
    }
}
