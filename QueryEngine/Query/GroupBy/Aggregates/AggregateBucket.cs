using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A base class for aggregates that compute results on bucket like storages.
    /// </summary>
    internal abstract class AggregateBucket : Aggregate
    {
        public AggregateBucket(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        /// <summary>
        /// Is called only on aggregates that are bound with the bucket type results.
        /// It computes the desired value from the containing expression with the given row and applies it to the aggregate.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="bucket"> A position to apply the computed value into. </param>
        public abstract void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket);
        /// <summary>
        /// A thread safe version of the simple apply method.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="bucket"> A position to apply the computed value into. </param>
        public abstract void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket);
    }

    /// <summary>
    /// Enables to compute expression values for the derived classes.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregate function. </typeparam>
    internal abstract class AggregateBucket<T> : AggregateBucket
    {
        protected ExpressionReturnValue<T> expr;

        public AggregateBucket(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder != null) this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
            else this.expr = null;
        }

        public override Type GetAggregateReturnType()
        {
            return typeof(T);
        }
    }
}
