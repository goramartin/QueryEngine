using System;

namespace QueryEngine
{
    /// <summary>
    /// A class that computes hash of an expression value with the fiven result row.
    /// The null values are represented as the 0 value.
    /// 
    /// Computes an hash of an given expression.
    /// The derived classes contain reference to the ExpressionEqualityComparer.
    /// If each time the hash function is called. If there is no cache in the derived classes, the hash only evaluates
    /// the given expression.
    /// If the hash is present, it evaluates the expression and stores the outcome of the expression into the inner variables
    /// of the ExpressionEqualityComparer.
    /// </summary>
    internal abstract class ExpressionHasher : IExpressionHasher
    {
        protected ExpressionHolder expressionHolder;

        protected ExpressionHasher(ExpressionHolder expressionHolder)
        {
            if (expressionHolder == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

            this.expressionHolder = expressionHolder;
        }

        public abstract int Hash(in TableResults.RowProxy row);

        public static ExpressionHasher Factory(ExpressionHolder expressionHolder)
        {
            if (expressionHolder.ExpressionType == typeof(int)) 
                return new ExpressionHasher<int>(expressionHolder);
            else if (expressionHolder.ExpressionType == typeof(string)) 
                return new ExpressionHasher<string>(expressionHolder);
            else throw new ArgumentException($"Expression hasher factory, trying to create hasher with unknown type = {expressionHolder.ExpressionType}.");
        }

        /// <summary>
        /// Clones the instance.
        /// Expects that the cache is different than the containing one in (this).
        /// </summary>
        public abstract ExpressionHasher Clone();
        public abstract void SetCache(ExpressionComparer cache);
    }

    internal class ExpressionHasher<T> : ExpressionHasher
    {
        protected ExpressionReturnValue<T> expr;
        protected ExpressionComparer<T> cache;

        public ExpressionHasher(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        public override int Hash(in TableResults.RowProxy row)
        {
            if (this.cache == null) return this.NonCachedHash(in row);
            else return this.CachedHash(in row);
        }

        private int NonCachedHash(in TableResults.RowProxy row)
        {
            if (this.expr.TryEvaluate(in row, out T returnValue)) 
                return returnValue.GetHashCode();
            else return 0;
        }

        private int CachedHash(in TableResults.RowProxy row)
        {
            bool ySuccess = this.expr.TryEvaluate(in row, out T yValue);
            int yRow = row.index;
            this.cache.SetYCache(ySuccess, yRow, yValue);

            if (ySuccess) return yValue.GetHashCode();
            else return 0;
        }

        public override ExpressionHasher Clone()
        {
            return new ExpressionHasher<T>(this.expressionHolder);
        }

        public override void SetCache(ExpressionComparer cache)
        {
            if (cache == null) this.cache = null;
            else this.cache = (ExpressionComparer<T>)cache;
        }
    }
}
