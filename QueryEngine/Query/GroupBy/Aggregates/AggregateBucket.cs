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

        /// <summary>
        /// Merges results of two buckets into the bucket1.
        /// It assumes that the results were set before.
        /// </summary>
        /// <param name="bucket1"> A bucket that will contain the final merged results. </param>
        /// <param name="bucket2"> A bucket that will provide value to merge for the bucket1. </param>
        public abstract void MergeTwoBuckets(AggregateBucketResult bucket1, AggregateBucketResult bucket2);

        /// <summary>
        /// Merges results of two buckets into the bucket1 with a thread safe manner.
        /// It assumes that the results were set before.
        /// </summary>
        /// <param name="bucket1"> A bucket that will contain the final merged results. </param>
        /// <param name="bucket2"> A bucket that will provide value to merge for the bucket1. </param>
        public abstract void MergeTwoBucketsThreadSage(AggregateBucketResult bucket1, AggregateBucketResult bucket2);
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
