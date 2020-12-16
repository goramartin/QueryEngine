using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A base class for every group result processor.
    /// </summary>
    internal abstract class GroupResultProcessor : ResultProcessor
    {
        protected List<Aggregate> aggregates { get; }
        protected List<ExpressionHolder> hashes { get; }
        protected bool InParallel { get; }
        protected int ThreadCount { get; }
        /// <summary>
        /// Represents a number of variables defined in the match clause of the query.
        /// </summary>
        protected int ColumnCount { get; }
        protected GroupResultProcessor(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, int columnCount)
        {
            this.aggregates = aggs;
            this.hashes = hashes;
            this.InParallel = helper.InParallel;
            this.ThreadCount = helper.ThreadCount;
            this.ColumnCount = columnCount;
        }

        protected virtual void CreateHashersAndComparers(out List<ExpressionEqualityComparer> comparers, out List<ExpressionHasher> hashers)
        {
            comparers = new List<ExpressionEqualityComparer>();
            hashers = new List<ExpressionHasher>();
            for (int i = 0; i < this.hashes.Count; i++)
            {
                comparers.Add(ExpressionEqualityComparer.Factory(this.hashes[i], this.hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(this.hashes[i], this.hashes[i].ExpressionType));
            }
        }
    }
}
