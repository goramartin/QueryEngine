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
    internal sealed class IntArrayMin : AggregateArray<int>
    {
        public IntArrayMin(ExpressionHolder holder) : base(holder)
        { }
        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                if (position == this.aggResults.Count) this.aggResults.Add(returnValue);
                else if (this.aggResults[position] > returnValue) this.aggResults[position] = returnValue;
            }
        }

        public override string ToString()
        {
            return "Min(" + this.expr.ToString() + ")";
        }

        public override void MergeOn(int into, int from)
        {
            if (into == this.aggResults.Count) this.aggResults.Add(this.mergingWithAggResults[from]);
            else  this.aggResults[into] = (this.aggResults[into] > this.mergingWithAggResults[from] ? this.mergingWithAggResults[from] : this.aggResults[into]);
        }

        public override string GetFuncName()
        {
            return "min";
        }
    }

    internal sealed class StrArrayMin : AggregateArray<string>
    {
        public StrArrayMin(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }
        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out string returnValue))
            {
                if (position == this.aggResults.Count) this.aggResults.Add(returnValue);
                else if (this.aggResults[position].CompareTo(returnValue) > 0) this.aggResults[position] = returnValue;
            }
        }

        public override string ToString()
        {
            return "Min(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int into, int from)
        {
            if (into == this.aggResults.Count) this.aggResults.Add(this.mergingWithAggResults[from]);
            else this.aggResults[into] = (this.aggResults[into].CompareTo(this.mergingWithAggResults[from]) > 0 ? this.mergingWithAggResults[from] : this.aggResults[into]);
        }

        public override string GetFuncName()
        {
            return "min";
        }
    }
}
