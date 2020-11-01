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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Counts a number of non null entries.
    /// </summary>
    internal class Count : Aggregate<int>
    {
        public Count(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder == null) this.IsAstCount = true;
        }

        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (!this.IsAstCount)
            {
                if ( this.expr.TryEvaluate(in row, out int returnValue))
                {
                    if (position == this.aggVals.Count) this.aggVals.Add(1);
                    else this.aggVals[position]++;
                }
            }
            else Inc(position);
        }

        public void Inc(int position)
        {
            if (position == this.aggVals.Count) this.aggVals.Add(1);
            else this.aggVals[position]++;
        }

        public void IncBy(int value, int position)
        {
            if (position == this.aggVals.Count) this.aggVals.Add(value);
            else this.aggVals[position] += value;
        }

        public override string ToString()
        {
            if (this.IsAstCount) return "Count(*)";
            else return "Count(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int position, Aggregate aggregate)
        {
            this.aggVals[position] += ((Count)aggregate).aggVals[position];
        }

        public override void MergeOn(int firstPosition, int secondPosition)
        {
            if (firstPosition == this.aggVals.Count) this.aggVals.Add(this.mergingWith[secondPosition]);
            else this.aggVals[firstPosition] += this.mergingWith[secondPosition];
        }
    }
}
