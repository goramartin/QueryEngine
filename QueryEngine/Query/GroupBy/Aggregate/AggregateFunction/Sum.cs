/*! \file
This file contains definition of a sum function.
The sum function can be specialised only on "number" types.
 */

using System.Threading;

namespace QueryEngine
{
    internal class IntSum : Aggregate<int>
    {
        public IntSum(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override string ToString()
        {
            return "Sum(" + this.expressionHolder.ToString() + ")";
        }

        public override string GetFuncName()
        {
            return "sum";
        }

        // Buckets
        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddInternal(ref ((AggregateBucketResult<long>)bucket).aggResult, returnValue);
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddThreadSafeInternal(ref ((AggregateBucketResult<long>)bucket).aggResult, returnValue);
        }
        public override void Apply(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddInternal(ref ((AggregateBucketResult<long>)bucket).aggResult, returnValue);
        }
        public override void ApplyThreadSafe(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddThreadSafeInternal(ref ((AggregateBucketResult<long>)bucket).aggResult, returnValue);
        }

        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            AddInternal(ref ((AggregateBucketResult<long>)bucket1).aggResult, ((AggregateBucketResult<long>)bucket2).aggResult);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            AddThreadSafeInternal(ref ((AggregateBucketResult<long>)bucket1).aggResult, ((AggregateBucketResult<long>)bucket2).aggResult);
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            AddInternal(ref ((AggregateBucketResult<long>)bucket).aggResult, ((AggregateListResults<long>)list).aggResults[position]);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            AddThreadSafeInternal(ref ((AggregateBucketResult<long>)bucket).aggResult, ((AggregateListResults<long>)list).aggResults[position]);
        }

        // Lists
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            var tmpList = (AggregateListResults<long>)list;
            if (position == tmpList.aggResults.Count) 
                tmpList.aggResults.Add(default);
            
            if (this.expr.TryEvaluate(in row, out int returnValue))
                tmpList.aggResults[position] += returnValue;
        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<long>)list1;
            var tmpList2 = (AggregateListResults<long>)list2;

            if (into == tmpList1.aggResults.Count) tmpList1.aggResults.Add(tmpList2.aggResults[from]);
            else tmpList1.aggResults[into] += tmpList2.aggResults[from];
        }

        // Arrays
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddThreadSafeInternal(ref ((AggregateArrayResults<long>)array).aggResults[position], returnValue);
        }


        private static void AddInternal(ref long placement, long value)
        {
            placement += value;
        }
        private static void AddThreadSafeInternal(ref long placement, long value)
        {
            Interlocked.Add(ref placement, value);
        }
    }
}
