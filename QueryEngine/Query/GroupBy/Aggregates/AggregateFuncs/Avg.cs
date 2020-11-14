/*! \file
This file contains definition of a avg function.
The avg function can be specialised only on "number" type.
The class computes avg using incremental approach.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public override void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<int>)bucket);
                tmpBucket.aggResult += returnValue;
                tmpBucket.eltUsed++;
            }
        }

        public override void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpBucket = ((AggregateBucketAvgResult<int>)bucket);
                Interlocked.Add(ref tmpBucket.aggResult, returnValue);
                Interlocked.Increment(ref tmpBucket.eltUsed);
            }
        }
        public override void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketAvgResult<int>)bucket1);
            var tmpBucket2 = ((AggregateBucketAvgResult<int>)bucket2);
            tmpBucket1.aggResult += tmpBucket2.aggResult;
            tmpBucket1.eltUsed += tmpBucket2.eltUsed;
        }

        public override void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2)
        {
            var tmpBucket1 = ((AggregateBucketAvgResult<int>)bucket1);
            var tmpBucket2 = ((AggregateBucketAvgResult<int>)bucket2);
            Interlocked.Add(ref tmpBucket1.aggResult, tmpBucket2.aggResult);
            Interlocked.Add(ref tmpBucket1.eltUsed, tmpBucket2.eltUsed);
        }

        public override void Apply(in TableResults.RowProxy row, AggregateListResults list, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                var tmpList = (AggregateListAvgResults<int>)list;

                if (position == tmpList.values.Count)
                {
                    tmpList.values.Add(returnValue);
                    tmpList.eltUsed.Add(1);
                }
                else
                {
                    tmpList.values[position] += returnValue;
                    tmpList.eltUsed[position]++;
                }
            }
        }

        public override void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from)
        {
            var tmpList1 = (AggregateListAvgResults<int>)list1;
            var tmpList2 = (AggregateListAvgResults<int>)list2;

            if (into == tmpList1.values.Count)
            {
                tmpList1.values.Add(tmpList2.values[from]);
                tmpList1.eltUsed.Add(tmpList2.eltUsed[from]);
            }
            else
            {
                tmpList1.values[into] += tmpList2.values[from];
                tmpList1.eltUsed[into] += tmpList2.eltUsed[from];
            }

        }

        public override void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketAvgResult<int>)bucket);
            var tmpList = ((AggregateListAvgResults<int>)list);
        
            Interlocked.Add(ref tmpBucket.aggResult, tmpList.values[position]);
            Interlocked.Add(ref tmpBucket.eltUsed, tmpList.eltUsed[position]);
        }

        public override void Merge(AggregateBucketResult bucket, AggregateListResults list, int position)
        {
            var tmpBucket = ((AggregateBucketAvgResult<int>)bucket);
            var tmpList = ((AggregateListAvgResults<int>)list);
            tmpBucket.aggResult += tmpList.values[position];
            tmpBucket.eltUsed += tmpList.eltUsed[position];
        }
    }
}
