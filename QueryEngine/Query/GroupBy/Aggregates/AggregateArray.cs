using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// A base class for aggregates that compute results on array like storages.
    /// </summary>
    internal abstract class AggregateArray : Aggregate
    {
        public AggregateArray(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }
        /// <summary>
        /// Is called only on aggregates that are bound with the array type results.
        /// It computes the desired value from the containing expression with the given row and applies it to the aggregate.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="position"> A position to apply the computed value into. </param>
        public abstract void Apply(in TableResults.RowProxy row, int position);
        /// <summary>
        /// Is called during merging in LocalGroupLocalMerge grouping.
        /// It merges aggregates values from two different result holders and merges them into the
        /// first one on the "into" position.
        /// </summary>
        /// <param name="into"> The position to merge value into. </param>
        /// <param name="from"> The position to merge value from. </param>
        public abstract void MergeOn(int into, int from);
        /// <summary>
        /// Sets internal list of aggregate values from the aggregate array results class to a 
        /// protected field. So that when computing aggregates, there is no need to cast the result holders every time.
        /// </summary>
        /// <param name="resultsStorage"> A aggregate array results holder. </param>
        public abstract void SetAggResults(AggregateArrayResults resultsStorage);
        /// <summary>
        /// Sets internal list of aggregate values that will be merged into the list that was set
        /// from the "SetAggResults" method.
        /// </summary>
        /// <param name="resultsStorage2"> A aggregate array results holder. </param>
        public abstract void SetMergingWith(AggregateArrayResults resultsStorage2);
        /// <summary>
        /// Unsets internal field of aggregate array results.
        /// </summary>
        public abstract void UnsetAggResults();
        /// <summary>
        /// Unsets internal field of aggregate array results to merge with.
        /// </summary>
        public abstract void UnsetMergingWith();
    }

    /// <summary>
    /// An aggregate fucntion base class for computing on array like storage.
    /// It enables to set direct reference to the result storage values.
    /// This enables to omit a lot of casts to the appropriate types.
    /// The methods SET stores the appropriate reference from the result storage holder.
    /// The methods UNSET unsets these references.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregate function. </typeparam>
    internal abstract class AggregateArray<T> : AggregateArray
    {
        protected ExpressionReturnValue<T> expr;
        protected List<T> aggResults = null;
        protected List<T> mergingWithAggResults = null;

        public AggregateArray(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder != null) this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
            else this.expr = null;
        }

        public override Type GetAggregateReturnType()
        {
            return typeof(T);
        }

        public override void SetMergingWith(AggregateArrayResults resultsStorage2)
        {
            this.mergingWithAggResults = ((AggregateArrayResults<T>)resultsStorage2).values;
        }

        public override void SetAggResults(AggregateArrayResults resultsStorage1)
        {
            this.aggResults = ((AggregateArrayResults<T>)resultsStorage1).values;
        }

        public override void UnsetAggResults()
        {
            this.aggResults = null;
        }

        public override void UnsetMergingWith()
        {
            this.mergingWithAggResults = null;
        }
    }
}
