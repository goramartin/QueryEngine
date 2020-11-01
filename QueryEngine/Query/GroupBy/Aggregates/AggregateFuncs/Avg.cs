/*! \file
This file contains definition of a avg function.
The avg function can be specialised only on "number" type.
The class computes avg using incremental approach.
 */using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A class computes avg using incremental approach.
    /// There is a need to rememeber the number of used elements for the already
    /// computed average.
    /// </summary>
    internal sealed class IntAvg : Aggregate<int>
    {
        private List<int> EltUsed = new List<int>();
        private List<int> mergingWithEltused = null;

        public IntAvg(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                if (position == this.aggVals.Count)
                {
                    this.aggVals.Add(returnValue);
                    this.EltUsed.Add(1);
                }
                else
                {   // n = a number of used elements for prev_avg
                    // prev_avg = a value of already computed average
                    // (new_avg = prev_avg * n + addedValue) / (n + 1)
                    this.aggVals[position] =
                        (this.aggVals[position] * this.EltUsed[position] + returnValue) / (this.EltUsed[position] + 1);
                    this.EltUsed[position]++;
                }
            }
        }

        public override string ToString()
        {
            return "Avg(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int position, Aggregate aggregate)
        {
            var tmp = ((IntAvg)aggregate);
            this.aggVals[position] = ((this.aggVals[position] * this.EltUsed[position]) + (tmp.aggVals[position] * tmp.EltUsed[position])) / (this.EltUsed[position] + tmp.EltUsed[position]);
        }

        public override void SetMergingWith(Aggregate aggregate)
        {
            base.SetMergingWith(aggregate);
            this.mergingWithEltused = ((IntAvg)aggregate).EltUsed;
        }

        public override void UnsetMergingWith()
        {
            base.UnsetMergingWith();
            this.mergingWithEltused = null;
        }

        public override void MergeOn(int firstPosition, int secondPosition)
        {
            if (firstPosition == this.aggVals.Count)
            {
                this.aggVals.Add(this.mergingWith[secondPosition]);
                this.EltUsed.Add(this.mergingWithEltused[secondPosition]);
            }
            else this.aggVals[firstPosition] = ((this.aggVals[firstPosition] * this.EltUsed[firstPosition]) + (this.mergingWith[secondPosition] * this.mergingWithEltused[secondPosition]))
                                            / (this.EltUsed[firstPosition] + this.mergingWithEltused[secondPosition]);
        }
    }
}
