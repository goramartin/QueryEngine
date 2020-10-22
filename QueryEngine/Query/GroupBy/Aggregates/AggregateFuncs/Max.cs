/*! \file
This file contains definition of a max function.
The max function can be specialised on any type.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class IntMax : Aggregate<int>
    {
        public IntMax(ExpressionHolder holder) : base(holder)
        { }
        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.exp.TryGetExpressionValue<int>(in row, out int returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(returnValue);
                else if (this.aggVals[position] < returnValue) this.aggVals[position] = returnValue;
            }
        }
    }

    /// <summary>
    /// A maximum function on string is computed with the help of 
    /// strA.CompareTo(strB)
    /// </summary>
    internal sealed class StrMax : Aggregate<string>
    {
        public StrMax(ExpressionHolder holder) : base(holder)
        { }
        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.exp.TryGetExpressionValue<string>(in row, out string returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(returnValue);
                else if (this.aggVals[position].CompareTo(returnValue) < 0) this.aggVals[position] = returnValue;
            }
        }
    }
}
