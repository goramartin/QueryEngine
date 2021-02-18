/*! \file
This file contains definition of a min and max function.
The min/max functions can be specialised on any type.
 */
using System;
using System.Threading;

namespace QueryEngine
{
    internal abstract class MinMaxBase<T> : Aggregate<T>
    {
        public MinMaxBase(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected abstract bool Compare(T x, T y);
        protected abstract bool CompareExchangeInternal(ref T value, T applied, T initial);


        // Buckets
        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
                ApplyInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, returnValue);
            }
        }
        public override void Apply(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
                ApplyInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, returnValue);
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
            MergeInternal(ref tmpBucket1.aggResult, ref tmpBucket1.isSet, tmpBucket2.aggResult, tmpBucket2.isSet);
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
            var tmpList = ((AggregateListResultsWithSetFlag<T>)list);
            MergeInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, tmpList.aggResults[position], tmpList.isSet[position]);
        }


        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketResultWithSetFlag<T>)bucket1);
            var tmpBucket2 = ((AggregateBucketResultWithSetFlag<T>)bucket2);

            if (!tmpBucket2.isSet) return;
            else
                ApplyThreadSafeInternal(ref tmpBucket1.aggResult, ref tmpBucket1.isSet, tmpBucket2.aggResult, tmpBucket1);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketResultWithSetFlag<T>)bucket);
            var tmpList = ((AggregateListResultsWithSetFlag<T>)list);
            
            if (!tmpList.isSet[position]) return;
            else
                ApplyThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.isSet, tmpList.aggResults[position], tmpBucket);
        }


        // Lists
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            var tmpList = (AggregateListResultsWithSetFlag<T>)list;
            // If it is the first entry.
            if (position  == tmpList.aggResults.Count)
            {
                tmpList.aggResults.Add(default);
                tmpList.isSet.Add(false);
            }

            if (this.expr.TryEvaluate(in row, out T returnValue))
            {
                if (!tmpList.isSet[position])
                {
                    tmpList.isSet[position] = true;
                    tmpList.aggResults[position] = returnValue;
                } 
                else if (Compare(tmpList.aggResults[position], returnValue)) 
                    tmpList.aggResults[position] = returnValue;
                else { }
            }
        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResultsWithSetFlag<T>)list1;
            var tmpList2 = (AggregateListResultsWithSetFlag<T>)list2;

            if (tmpList1.aggResults.Count == into)
            {
                tmpList1.aggResults.Add(default);
                tmpList1.isSet.Add(false);
            }

            if (tmpList1.isSet[into] && tmpList2.isSet[from])
            {
                if (Compare(tmpList1.aggResults[into], tmpList2.aggResults[from]))
                    tmpList1.aggResults[into] = tmpList2.aggResults[from];
                else { }
            }
            else if (tmpList1.isSet[into] && !tmpList2.isSet[from]) return;
            else
            {
                tmpList1.isSet[into] = true;
                tmpList1.aggResults[into] = tmpList2.aggResults[from];
            }
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


        protected void MergeInternal(ref T value1, ref bool isSet1, T value2, bool isSet2)
        {
            // Both are set, choose the smaller/larger.
            if (isSet1 && isSet2)
            {
                if (Compare(value1, value2)) value1 = value2;
                else { }
            }
            // The second has no result.
            else if (isSet1 && !isSet2) return;
            // Move the result from the second to the first.
            else
            {
                isSet1 = true;
                value1 = value2;
            }
        }
        protected void ApplyInternal(ref T value, ref bool isSet, T applied)
        {
            if (isSet)
            {
                if (Compare(value, applied)) value = applied;
                else { }
            }
            else
            {
                value = applied;
                isSet = true;
            }
        }
        
        
        protected void CompareExchange(ref T value, T applied)
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
            while (!CompareExchangeInternal(ref value, replacement, initialValue));
        }
        protected void ApplyThreadSafeInternal(ref T value, ref bool isSet, T applied, object lockingObject)
        {
            bool wasSet = false;
            while (!wasSet)
            {
                if (isSet)
                {
                    CompareExchange(ref value, applied);
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

        protected override bool CompareExchangeInternal(ref int value, int applied, int initial)
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
            return (String.Compare(x, y, StringComparison.Ordinal) > 0);
        }

        protected override bool CompareExchangeInternal(ref string value, string applied, string initial)
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

        protected override bool CompareExchangeInternal(ref int value, int applied, int initial)
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
            return (String.Compare(x, y, StringComparison.Ordinal) < 0);
        }

        protected override bool CompareExchangeInternal(ref string value, string applied, string initial)
        {
            return (initial == Interlocked.CompareExchange(ref value, applied, initial));
        }
    }
}
