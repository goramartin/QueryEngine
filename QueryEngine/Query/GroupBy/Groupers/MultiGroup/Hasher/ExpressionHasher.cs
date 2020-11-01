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
    internal abstract class ExpressionHasher : IRowHasher
    {
        protected ExpressionHolder expressionHolder;

        protected ExpressionHasher(ExpressionHolder expressionHolder)
        {
            this.expressionHolder = expressionHolder;
        }

        public abstract int Hash(in TableResults.RowProxy row);

        public static ExpressionHasher Factory(ExpressionHolder expressionHolder, Type type, ExpressionEqualityComparer cache)
        {
            if (type == typeof(int)) return new ExpressionIntegerHasher(expressionHolder, cache);
            else if (type == typeof(string)) return new ExpressionStringHasher(expressionHolder, cache);
            else throw new ArgumentException($"Expression hasher factory, trying to create hasher with unknown type = {type}.");
        }

        /// <summary>
        /// Clones the instance.
        /// Expects that the cache is different than the containing one in (this).
        /// </summary>
        public abstract ExpressionHasher Clone(ExpressionEqualityComparer cache);
    }

    internal class ExpressionIntegerHasher : ExpressionHasher
    {
        protected ExpressionReturnValue<int> expr;
        protected ExpressionIntegerEqualityComparer cache;

        public ExpressionIntegerHasher(ExpressionHolder expressionHolder, ExpressionEqualityComparer cache) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<int>)expressionHolder.Expr;
            this.cache = (ExpressionIntegerEqualityComparer)cache;

        }
        
        public override int Hash(in TableResults.RowProxy row)
        {
            if (this.cache == null)
            {
                if (this.expr.TryEvaluate(in row, out int retValue)) return retValue.GetHashCode();
                else return 0;
            } else
            {
                this.cache.successLastX = this.expr.TryEvaluate(in row, out this.cache.resultLastX);
                this.cache.rowlastX = row.index;
                if (this.cache.successLastX) return this.cache.resultLastX.GetHashCode();
                else return 0;
            }
        }

        public override ExpressionHasher Clone(ExpressionEqualityComparer cache)
        {
            return new ExpressionIntegerHasher(this.expressionHolder, cache);
        }
    }

    internal class ExpressionStringHasher : ExpressionHasher
    {
        protected ExpressionReturnValue<string> expr;
        protected ExpressionStringEqualityComparer cache;

        public ExpressionStringHasher(ExpressionHolder expressionHolder, ExpressionEqualityComparer cache) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<string>)expressionHolder.Expr;
            this.cache = (ExpressionStringEqualityComparer)cache;
        }


        public override int Hash(in TableResults.RowProxy row)
        {
            if (this.cache == null)
            {
                if (this.expr.TryEvaluate(in row, out string retValue)) return retValue.GetHashCode();
                else return 0;
            } else
            {
                this.cache.successLastX = this.expr.TryEvaluate(in row, out this.cache.resultLastX);
                this.cache.rowlastX = row.index;
                if (this.cache.successLastX) return this.cache.resultLastX.GetHashCode();
                else return 0;
            }
        }

        public override ExpressionHasher Clone(ExpressionEqualityComparer cache)
        {
            return new ExpressionStringHasher(this.expressionHolder, cache);
        }
    }

}
