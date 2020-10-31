using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    interface IRowHasher
    {
        int Hash(in TableResults.RowProxy row);
    }

    /// <summary>
    /// Creates a hash for a given row.
    /// For null values (missing property on an element) the returned hash is 0.
    /// </summary>
    internal class RowHasher : IRowHasher
    {
        protected List<ExpressionHasher> hashers;

        public RowHasher(List<ExpressionHasher> hashers)
        {
            this.hashers = hashers;
        }

        public int Hash(in TableResults.RowProxy row)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < this.hashers.Count; i++)
                    hash = 33 * hash + this.hashers[i].Hash(in row);
                return hash;
            }
        }


        public RowHasher Clone()
        {
            var tmp = new List<ExpressionHasher>();
            for (int i = 0; i < 0; i++)
                tmp.Add(this.hashers[i].Clone());

            return new RowHasher(tmp);
        }
    }

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
