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
    /// A class computes avg by remembering the number of values and their sum.
    /// The final result must be computed separately when accessing the aggregate results.
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
                { 
                    this.aggResults[position] += returnValue;
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
            else
            { 
                this.aggResults[into] += this.mergingWithAggResults[from];
                this.EltUsed[into] += this.mergingWithEltUsed[from];
            }

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
