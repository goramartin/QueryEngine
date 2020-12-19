using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.expressionHolder = expressionHolder;
        }

        public abstract int Hash(in TableResults.RowProxy row);

        public static ExpressionHasher Factory(ExpressionHolder expressionHolder, Type type)
        {
            if (type == typeof(int)) 
                return new ExpressionHasher<int>(expressionHolder);
            else if (type == typeof(string)) 
                return new ExpressionHasher<string>(expressionHolder);
            else throw new ArgumentException($"Expression hasher factory, trying to create hasher with unknown type = {type}.");
        }

        /// <summary>
        /// Clones the instance.
        /// Expects that the cache is different than the containing one in (this).
        /// </summary>
        public abstract ExpressionHasher Clone();
        public abstract void SetCache(ExpressionEqualityComparer cache);
    }

    internal class ExpressionHasher<T> : ExpressionHasher
    {
        protected ExpressionReturnValue<T> expr;
        protected ExpressionEqualityComparer<T> cache;

        public ExpressionHasher(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        public override int Hash(in TableResults.RowProxy row)
        {
            if (this.cache == null)
            {
                if (this.expr.TryEvaluate(in row, out T returnValue)) return returnValue.GetHashCode();
                else return 0;
            }
            else
            {
                this.cache.lastYSuccess = this.expr.TryEvaluate(in row, out this.cache.lastYValue);
                this.cache.lastYRow = row.index;
                if (this.cache.lastYSuccess) return this.cache.lastYValue.GetHashCode();
                else return 0;
            }
        }

        public override ExpressionHasher Clone()
        {
            return new ExpressionHasher<T>(this.expressionHolder);
        }

        public override void SetCache(ExpressionEqualityComparer cache)
        {
            if (cache == null) this.cache = null;
            else this.cache = (ExpressionEqualityComparer<T>)cache;
        }
    }
}
