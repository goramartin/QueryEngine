/*! \file
This file contains definition of a count function.
The count function can be specialised only on "number" type.

The count function have multiple uses.
1. count(*) - the function counts each row as a valid input.
2. count(x.PropName) - the function needs to evaluete the 
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
    /// Counts a number of not null entries.
    /// </summary>
    internal class Count : Aggregate<int>
    {
        public Count(ExpressionHolder holder) : base(holder)
        {
            this.IsAstCount = true;
        }

        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.exp.TryGetExpressionValue<int>(in row, out int returnValue))
            {
                if (position == this.aggVals.Count) this.aggVals.Add(1);
                else this.aggVals[position]++;
            }  
        }

        public void Inc(int position)
        {
            if (position == this.aggVals.Count) this.aggVals.Add(1);
            else this.aggVals[position]++;
        }

        public void IncBy(int position, int value)
        {
            if (position == this.aggVals.Count) this.aggVals.Add(value);
            else this.aggVals[position] += value;
        }
    }
}
