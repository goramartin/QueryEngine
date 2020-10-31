/*! \file
This file contains definition of a min function.
The min function can be specialised on any type.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class IntMin : Aggregate<int>
    {
        public IntMin(ExpressionHolder holder) : base(holder)
        { }
        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(returnValue);
                else if (this.aggVals[position] > returnValue) this.aggVals[position] = returnValue;
            }
        }

        public override string ToString()
        {
            return "Min(" + this.expr.ToString() + ")";
        }

        public override void MergeOn(int position, Aggregate aggregate)
        {
            var tmp = ((IntMin)aggregate).aggVals[position];
            this.aggVals[position] = (this.aggVals[position]  > tmp ? tmp : this.aggVals[position]);
        }
    }

    /// <summary>
    /// A minimum function on string is computed with the help of 
    /// strA.CompareTo(strB)
    /// </summary>
    internal sealed class StrMin : Aggregate<string>
    {
        public StrMin(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }
        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(returnValue);
                else if (this.aggVals[position].CompareTo(returnValue) > 0) this.aggVals[position] = returnValue;
            }
        }

        public override string ToString()
        {
            return "Min(" + this.expressionHolder.ToString() + ")";
        }


        public override void MergeOn(int position, Aggregate aggregate)
        {
        var tmp = ((StrMin)aggregate).aggVals[position];
            this.aggVals[position] = (this.aggVals[position].CompareTo(tmp) > 0 ? tmp : this.aggVals[position]);
        }
    }
}
