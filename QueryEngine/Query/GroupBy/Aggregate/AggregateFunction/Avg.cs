/*! \file
This file contains definition of a avg function.
The avg function can be specialised only on "number" type.
The class computes avg using incremental approach.
*/

using System.Threading;

namespace QueryEngine 
{
    class IntAvg : Aggregate<int>
    {
        public IntAvg(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override string ToString()
        {
            return "Avg(" + this.expressionHolder.ToString() + ")";
        }
        public override string GetFuncName()
        {
            return "avg";
        }
       
        // Buckets
        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<long>)bucket);
                AddInternal(ref tmpBucket.aggResult, ref tmpBucket.eltsUsed, returnValue, 1);
            }
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<long>)bucket);
                AddThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.eltsUsed, returnValue, 1);
            }
        }
        public override void Apply(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<long>)bucket);
                AddInternal(ref tmpBucket.aggResult, ref tmpBucket.eltsUsed, returnValue, 1);
            }
        }
        public override void ApplyThreadSafe(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<long>)bucket);
                AddThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.eltsUsed, returnValue, 1);
            }
        }

        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketAvgResult<long>)bucket1);
            var tmpBucket2 = ((AggregateBucketAvgResult<long>)bucket2);
            AddInternal(ref tmpBucket1.aggResult, ref tmpBucket1.eltsUsed, tmpBucket2.aggResult, tmpBucket2.eltsUsed);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketAvgResult<long>)bucket1);
            var tmpBucket2 = ((AggregateBucketAvgResult<long>)bucket2);
            AddThreadSafeInternal(ref tmpBucket1.aggResult, ref tmpBucket1.eltsUsed, tmpBucket2.aggResult, tmpBucket2.eltsUsed);
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketAvgResult<long>)bucket);
            var tmpList = ((AggregateListAvgResults<long>)list);
            AddInternal(ref tmpBucket.aggResult, ref tmpBucket.eltsUsed, tmpList.aggResults[position], tmpList.eltsUsed[position]);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketAvgResult<long>)bucket);
            var tmpList = ((AggregateListAvgResults<long>)list);
            AddThreadSafeInternal(ref tmpBucket.aggResult, ref tmpBucket.eltsUsed, tmpList.aggResults[position], tmpList.eltsUsed[position]);
        }

        // Lists
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            var tmpList = (AggregateListAvgResults<long>)list;
            if (position == tmpList.aggResults.Count)
            {
                tmpList.aggResults.Add(default);
                tmpList.eltsUsed.Add(default);
            }

            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                tmpList.aggResults[position] += returnValue;
                tmpList.eltsUsed[position]++;
            }
        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListAvgResults<long>)list1;
            var tmpList2 = (AggregateListAvgResults<long>)list2;

            if (into == tmpList1.aggResults.Count)
            {
                tmpList1.aggResults.Add(tmpList2.aggResults[from]);
                tmpList1.eltsUsed.Add(tmpList2.eltsUsed[from]);
            }
            else
            {
                tmpList1.aggResults[into] += tmpList2.aggResults[from];
                tmpList1.eltsUsed[into] += tmpList2.eltsUsed[from];
            }

        }
        
        // Arrays
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpArray = ((AggregateArrayAvgResults<long>)array);
                AddThreadSafeInternal(ref tmpArray.aggResults[position], ref tmpArray.eltsUsed[position], returnValue , 1);
            }
        }

        private static void AddInternal(ref long placement, ref int eltsUsed, long value, int eltsValue)
        {
            placement += value;
            eltsUsed += eltsValue;
        }
        private static void AddThreadSafeInternal(ref long placement, ref int eltsUsed, long value, int eltsValue)
        {
            Interlocked.Add(ref placement, value);
            Interlocked.Add(ref eltsUsed, eltsValue);
        }
    }
}
