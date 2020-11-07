/*! \file
This file contains definition of a sum function.
The sum function can be specialised only on "number" types.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class IntArraySum : AggregateArray<int>
    {
        public IntArraySum(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }


        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                if (position == this.aggResults.Count) this.aggResults.Add(returnValue);
                else this.aggResults[position] += returnValue;
            }
        }


        public override string ToString()
        {
            return "Sum(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int into, int from)
        {
            if (into == this.aggResults.Count) this.aggResults.Add(this.mergingWithAggResults[from]);
            else this.aggResults[into] += this.mergingWithAggResults[from];
        }

        public override string GetFuncName()
        {
            return "sum";
        }
    }
}
