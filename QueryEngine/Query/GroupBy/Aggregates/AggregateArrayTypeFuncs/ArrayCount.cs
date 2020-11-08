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
    internal class ArrayCount : AggregateArray<int>
    {
        public ArrayCount(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder == null) this.IsAstCount = true;
        }

        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (!this.IsAstCount)
            {
                if ( this.expr.TryEvaluate(in row, out int returnValue))
                {
                    if (position == this.aggResults.Count) this.aggResults.Add(1);
                    else this.aggResults[position]++;
                }
            }
            else Inc(position);
        }

        public void Inc(int position)
        {
            if (position == this.aggResults.Count) this.aggResults.Add(1);
            else this.aggResults[position]++;
        }

        public void IncBy(int value, int position)
        {
            if (position == this.aggResults.Count) this.aggResults.Add(value);
            else this.aggResults[position] += value;
        }

        public override string ToString()
        {
            if (this.IsAstCount) return "Count(*)";
            else return "Count(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int into, int from)
        {
            if (into == this.aggResults.Count) this.aggResults.Add(this.mergingWithAggResults[from]);
            else this.aggResults[into] += this.mergingWithAggResults[from];
        }

        public override string GetFuncName()
        {
            return "count";
        }
    }
}
