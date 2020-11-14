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

        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                ((AggregateBucketResult<int>)bucket).aggResult += returnValue;
        }

        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
                Interlocked.Add(ref ((AggregateBucketResult<int>)bucket).aggResult, returnValue);
        }

        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            ((AggregateBucketResult<int>)bucket1).aggResult += ((AggregateBucketResult<int>)bucket2).aggResult;
        }

        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            Interlocked.Add(ref ((AggregateBucketResult<int>)bucket1).aggResult, ((AggregateBucketResult<int>)bucket2).aggResult);
        }

        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
                if (this.expr.TryEvaluate(in row, out int returnValue))
                {
                    var tmpList = (AggregateListResults<int>)list;
                    if (position == tmpList.values.Count) tmpList.values.Add(returnValue);
                    else tmpList.values[position] += returnValue;
                }
        }

        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListResults<int>)list1;
            var tmpList2 = (AggregateListResults<int>)list2;

            if (into == tmpList1.values.Count) tmpList1.values.Add(tmpList2.values[from]);
            else tmpList1.values[into] += tmpList2.values[from];
        }

        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            Interlocked.Add(ref ((AggregateBucketResult<int>)bucket).aggResult,((AggregateListResults<int>)list).values[position]);
        }

        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            ((AggregateBucketResult<int>)bucket).aggResult += ((AggregateListResults<int>)list).values[position];
        }
    }
}
