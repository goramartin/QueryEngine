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
        public IntSum(ExpressionHolder holder) : base(holder)
        { }


        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.exp.TryGetExpressionValue<int>(in row, out int returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(returnValue);
                else this.aggVals[position] += returnValue;
            }
        }
    }
}
