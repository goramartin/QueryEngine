/*! \file
This file contains definition of a avg function.
The avg function can be specialised only on "number" type.
The class computes avg using incremental approach.
*/

using System;
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
    internal sealed class IntArrayAvg : AggregateArray<int>
    {
        private List<int> EltUsed = null;
        private List<int> mergingWithEltUsed = null;

        public IntArrayAvg(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        public override void Apply(in TableResults.RowProxy row, int position)
        {
            if (this.expr.TryEvaluate(in row, out int returnValue))
            {
                if (position == this.aggResults.Count)
                {
                    this.aggResults.Add(returnValue);
                    this.EltUsed.Add(1);
                }
                else
                {   // n = a number of used elements for prev_avg
                    // prev_avg = a value of already computed average
                    // (new_avg = prev_avg * n + addedValue) / (n + 1)
                    this.aggResults[position] = (this.aggResults[position] * this.EltUsed[position] + returnValue) / (this.EltUsed[position] + 1);
                    this.EltUsed[position]++;
                }
            }
        }

        public override string ToString()
        {
            return "Avg(" + this.expressionHolder.ToString() + ")";
        }

        public override void MergeOn(int into, int from)
        {
            if (into == this.aggResults.Count)
            {
                this.aggResults.Add(this.mergingWithAggResults[from]);
                this.EltUsed.Add(this.mergingWithEltUsed[from]);
            }
            // resAvg = (( intoAvg * intoN ) + (fromAvg * fromN)) / (intoN + fromN)
            else this.aggResults[into] = ((this.aggResults[into] * this.EltUsed[into]) + (this.mergingWithAggResults[from] * this.mergingWithEltUsed[from])) / (this.EltUsed[into] + this.mergingWithEltUsed[from]);
        }

        public override void SetAggResults(AggregateArrayResults resultsStorage1)
        {
            base.SetAggResults(resultsStorage1);
            this.EltUsed = ((AggregateArrayAvgResults<int>)resultsStorage1).eltUsed;
        }

        public override void SetMergingWith(AggregateArrayResults resultsStorage2)
        {
            base.SetMergingWith(resultsStorage2);
            this.mergingWithEltUsed = ((AggregateArrayAvgResults<int>)resultsStorage2).eltUsed;
        }

        public override void UnsetAggResults()
        {
            base.UnsetAggResults();
            this.EltUsed = null;
        }

        public override void UnsetMergingWith()
        {
            base.UnsetMergingWith();
            this.mergingWithEltUsed = null;
        }

        public override string GetFuncName()
        {
            return "avg";
        }
    }
}
