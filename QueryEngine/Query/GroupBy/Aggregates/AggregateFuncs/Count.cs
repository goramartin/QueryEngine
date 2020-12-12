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
    internal class Count : Aggregate<int>
    {
        public Count(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder == null) this.IsAstCount = true;
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
        public void IncBy(int value, AggregateBucketResult bucket)
        {
            var tmpBucket = (AggregateBucketResult<int>)bucket;
            tmpBucket.aggResult += value;
        }
        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (!this.IsAstCount)
            {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                   IncrementInternal(ref ((AggregateBucketResult<int>)bucket).aggResult);
            }
            else IncrementInternal(ref ((AggregateBucketResult<int>)bucket).aggResult);
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (!this.IsAstCount)
            {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                    IncrementThreadSafeInternal(ref ((AggregateBucketResult<int>)bucket).aggResult);
            }
            else IncrementThreadSafeInternal(ref ((AggregateBucketResult<int>)bucket).aggResult);
        }
        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            AddInternal(ref ((AggregateBucketResult<int>)bucket1).aggResult,((AggregateBucketResult<int>)bucket2).aggResult);
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
        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<int>)list1;
            var tmpList2 = (AggregateListResults<int>)list2;

            if (into == tmpList1.aggResults.Count) tmpList1.aggResults.Add(tmpList2.aggResults[from]);
            else tmpList1.aggResults[into] += tmpList2.aggResults[from];
        }
        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            if (!this.IsAstCount)
            {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                {
                    var tmpList = (AggregateListResults<int>)list;
                    if (position == tmpList.aggResults.Count) tmpList.aggResults.Add(1);
                    else tmpList.aggResults[position]++;
                }
            }
            else
            {
                var tmpList = (AggregateListResults<int>)list;
                if (position == tmpList.aggResults.Count) tmpList.aggResults.Add(1);
                else tmpList.aggResults[position]++;
            }
        }
        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position)
        {
             if (!this.IsAstCount)
             {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                    IncrementThreadSafeInternal(ref ((AggregateArrayResults<int>)array).aggResults[position]);
             }
             else IncrementThreadSafeInternal(ref ((AggregateArrayResults<int>)array).aggResults[position]);
        }


        private void AddInternal(ref int placement, int value)
        {
            placement += value;
        }
        private void AddThreadSafeInternal(ref int placement, int value)
        {
            Interlocked.Add(ref placement, value);
        }
        private void IncrementInternal(ref int placement)
        {
            placement++;
        }
        private void IncrementThreadSafeInternal(ref int placement)
        {
            Interlocked.Increment(ref placement);
        }
    }
}
