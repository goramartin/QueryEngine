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
                AddInternal(ref ((AggregateBucketResult<int>)bucket).aggResult, returnValue);
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddThreadSafeInternal(ref ((AggregateBucketResult<int>)bucket).aggResult, returnValue);
        }
        public override void Apply(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddInternal(ref ((AggregateBucketResult<int>)bucket).aggResult, returnValue);
        }
        public override void ApplyThreadSafe(in Element[] row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddThreadSafeInternal(ref ((AggregateBucketResult<int>)bucket).aggResult, returnValue);
        }

        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            AddInternal(ref ((AggregateBucketResult<int>)bucket1).aggResult, ((AggregateBucketResult<int>)bucket2).aggResult);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            AddThreadSafeInternal(ref ((AggregateBucketResult<int>)bucket1).aggResult, ((AggregateBucketResult<int>)bucket2).aggResult);
        }
        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            AddInternal(ref ((AggregateBucketResult<int>)bucket).aggResult, ((AggregateListResults<int>)list).aggResults[position]);
        }
        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            AddThreadSafeInternal(ref ((AggregateBucketResult<int>)bucket).aggResult, ((AggregateListResults<int>)list).aggResults[position]);
        }

        // Lists
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                {
                    var tmpList = (AggregateListResults<int>)list;
                    if (position == tmpList.aggResults.Count) tmpList.aggResults.Add(returnValue);
                    else tmpList.aggResults[position] += returnValue;
                }
        }
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<int>)list1;
            var tmpList2 = (AggregateListResults<int>)list2;

            if (into == tmpList1.aggResults.Count) tmpList1.aggResults.Add(tmpList2.aggResults[from]);
            else tmpList1.aggResults[into] += tmpList2.aggResults[from];
        }

        // Arrays
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                AddThreadSafeInternal(ref ((AggregateArrayResults<int>)array).aggResults[position], returnValue);
        }





        private void AddInternal(ref int placement, int value)
        {
            placement += value;
        }
        private void AddThreadSafeInternal(ref int placement, int value)
        {
            Interlocked.Add(ref placement, value);
        }
    }
}
