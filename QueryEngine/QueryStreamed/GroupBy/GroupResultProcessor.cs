using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A base class for every group result processor.
    /// </summary>
    internal abstract class GroupResultProcessor : ResultProcessor
    {
        protected Aggregate[] aggregates { get; }
        protected ExpressionHolder[] hashes { get; }
        protected bool InParallel { get; }
        protected int ThreadCount { get; }
        /// <summary>
        /// Represents a number of variables defined in the match clause of the query.
        /// </summary>
        protected int ColumnCount { get; }
        protected GroupResultProcessor(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, int columnCount)
        {
            this.aggregates = aggs;
            this.hashes = hashes;
            this.InParallel = helper.InParallel;
            this.ThreadCount = helper.ThreadCount;
            this.ColumnCount = columnCount;
        }

        protected virtual void CreateHashersAndComparers(out ExpressionEqualityComparer[] comparers, out ExpressionHasher[] hashers)
        {
            comparers = new ExpressionEqualityComparer[this.hashes.Length];
            hashers = new ExpressionHasher[this.hashes.Length];
            for (int i = 0; i < this.hashes.Length; i++)
            {
                comparers[i] = (ExpressionEqualityComparer.Factory(this.hashes[i], this.hashes[i].ExpressionType));
                hashers[i] = (ExpressionHasher.Factory(this.hashes[i], this.hashes[i].ExpressionType));
            }
        }
    }
}
