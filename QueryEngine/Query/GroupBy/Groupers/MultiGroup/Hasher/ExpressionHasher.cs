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
    /// </summary>
    internal abstract class ExpressionHasher : IRowHasher
    {
        protected ExpressionHolder expressionHolder;

        protected ExpressionHasher(ExpressionHolder expressionHolder)
        {
            this.expressionHolder = expressionHolder;
        }

        public abstract int Hash(in TableResults.RowProxy row);

        public static ExpressionHasher Factory(Type type, ExpressionHolder expressionHolder)
        {
            if (type == typeof(int)) return new ExpressionHasher<int>(expressionHolder);
            else if (type == typeof(string)) return new ExpressionHasher<string>(expressionHolder);
            else throw new ArgumentException($"Expression hasher factory, trying to create hasher with unknown type = {type}.");
        }

        public abstract ExpressionHasher Clone();
    }

    internal class ExpressionHasher<T> : ExpressionHasher
    {
        protected ExpressionReturnValue<T> expr;
        // To do add cache to equality comparer

        public ExpressionHasher(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        public override int Hash(in TableResults.RowProxy row)
        {
            if (this.expr.TryEvaluate(in row, out T retValue)) return retValue.GetHashCode();
            else return 0;
        }

        public override ExpressionHasher Clone()
        {
            return new ExpressionHasher<T>(this.expressionHolder);
        }
    }
}
