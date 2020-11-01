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
    internal sealed class IntSum : Aggregate<int>
    {
        public IntSum(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }


        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(returnValue);
                else this.aggVals[position] += returnValue;
            }
        }


        public override string ToString()
        {
            return "Sum(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int position, Aggregate aggregate)
        {
            this.aggVals[position] += ((IntSum)aggregate).aggVals[position];
        }

        public override void MergeOn(int firstPosition, int secondPosition)
        {
            if (firstPosition == this.aggVals.Count) this.aggVals.Add(this.mergingWith[secondPosition]);
            else this.aggVals[firstPosition] += this.mergingWith[secondPosition];
        }
    }
}
