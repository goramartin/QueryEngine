using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for expression equality value comparing.
    /// Notice the similarity between the ExpressionComparer and ExpressionEqualityComparer.
    /// However, these classes have different semantics.
    /// Each class contains an expression that will be evaluated with given rows.
    /// The classes are not generic because there is a need to use compare operators and avoid unneccessary
    /// passing arguments to another virual method.
    /// 
    /// It also contains basics for caching of the last X row.
    /// It remembers the position of that row and also the outcome of the expression evaluation.
    /// The exact result is expected to be stored in the derived class.
    /// They are public so the hasher classes can set the cache when they compute the hash function (they will use the same expressions).
    /// </summary>
    internal abstract class ExpressionEqualityComparer
    {
        protected readonly ExpressionHolder expressionHolder;
        protected readonly List<int> usedVars;
        public int rowlastY = -1;
        public bool successLastY = false;

        /// <summary>
        /// Constructs expression equality comparer.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated during comparing. </param>
        protected ExpressionEqualityComparer(ExpressionHolder expressionHolder)
        {
            this.expressionHolder = expressionHolder;
            this.usedVars = expressionHolder.CollectUsedVars(new List<int>());
        }

        public abstract bool Equals(in TableResults.RowProxy x, in TableResults.RowProxy y);

        /// <summary>
        /// Expression equality comparer factory.
        /// Creates a templated expression equality comparer based on a given type.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated. </param>
        /// <param name="typeOfExpression"> Type of equality comparer. </param>
        /// <returns> Specialised equality comparer. </returns>
        public static ExpressionEqualityComparer Factory(ExpressionHolder expressionHolder, Type typeOfExpression)
        {
            if (typeOfExpression == typeof(int))
                return new ExpressionIntegerEqualityComparer(expressionHolder);
            else if (typeOfExpression == typeof(string))
                return new ExpressionStringEqualityComparer(expressionHolder);
            else throw new ArgumentException($"Expression equality comparer factory, unknown type passed to a expression comparer factory.");
        }

        /// <summary>
        /// Checks whether used variables inside expression are same.
        /// In case there are the same, the expression should give the same 
        /// result.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row.</param>
        /// <returns> True if all used variables are the same. </returns>
        protected bool AreIdenticalVars(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            for (int i = 0; i < usedVars.Count; i++)
                if (x[i].ID != y[i].ID) return false;

            return true;
        }

        public abstract ExpressionEqualityComparer Clone();
    }

    internal class ExpressionIntegerEqualityComparer : ExpressionEqualityComparer
    {
        // To avoid casting every time Holder.TryGetValue()
        ExpressionReturnValue<int> expr;
        public int resultLastY = default;

        public ExpressionIntegerEqualityComparer(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<int>)expressionHolder.Expr;
        }

        /// <summary>
        /// Tries to evaluate containing expression with given rows.
        /// Caching of the last used X row.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row. </param>
        /// <returns>  True if the expressions are equal otherwise false. </returns>
        public override bool Equals(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            // Check if used variables in expression are same
            if (AreIdenticalVars(x, y)) return true;

            var xSuccess = this.expr.TryEvaluate(y, out int xValue);
            if (this.rowlastY != y.index)
            {
                this.successLastY = this.expr.TryEvaluate(x, out this.resultLastY);
                rowlastY = y.index;
            }

            if (!this.successLastY && !xSuccess) return true;
            else return this.resultLastY == xValue;
        }

        public override ExpressionEqualityComparer Clone()
        {
            return new ExpressionIntegerEqualityComparer(this.expressionHolder);
        }

    }

    internal class ExpressionStringEqualityComparer : ExpressionEqualityComparer
    {
        // To avoid casting every time Holder.TryGetValue()
        ExpressionReturnValue<string> expr;
        public string resultLastY = null;

        public ExpressionStringEqualityComparer(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<string>)expressionHolder.Expr;
        }

        /// <summary>
        /// Tries to evaluate containing expression with given rows.
        /// Caching of the last used X row.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row. </param>
        /// <returns>  True if the expressions are equal otherwise false. </returns>
        public override bool Equals(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            // Check if used variables in expression are same
            if (AreIdenticalVars(x, y)) return true;

            var xSuccess = this.expr.TryEvaluate(y, out string xValue);
            if (this.rowlastY != y.index)
            {
                this.successLastY = this.expr.TryEvaluate(x, out this.resultLastY);
                rowlastY = y.index;
            }

            if (!this.successLastY && !xSuccess) return true;
            else return this.resultLastY == xValue;
        }

        public override ExpressionEqualityComparer Clone()
        {
            return new ExpressionStringEqualityComparer(this.expressionHolder);
        }
    }
}
