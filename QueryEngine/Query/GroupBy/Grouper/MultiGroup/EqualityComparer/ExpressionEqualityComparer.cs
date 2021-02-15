using System;
using System.Collections.Generic;

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
    internal abstract class ExpressionEqualityComparer : IExpressionEqualityComparer
    {
        protected ExpressionHolder expressionHolder;
        protected int[] usedVars;
        protected ExpressionHasher boundHasher;
        public int lastYRow = -1;
        public bool lastYSuccess = false;

        /// <summary>
        /// Constructs expression equality comparer.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated during comparing. </param>
        protected ExpressionEqualityComparer(ExpressionHolder expressionHolder)
        {
            this.expressionHolder = expressionHolder;
            this.usedVars = expressionHolder.CollectUsedVars(new List<int>()).ToArray();
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

        public abstract ExpressionEqualityComparer Clone();
        public abstract void SetCache(ExpressionHasher cache);
    }

    internal abstract class ExpressionEqualityComparer<T> : ExpressionEqualityComparer
    {
        ExpressionReturnValue<T> expr;
        public T lastYValue = default;
        public ExpressionEqualityComparer(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        /// <summary>
        /// Tries to evaluate containing expression with given rows.
        /// Caching of the last used X row.
        /// If the results are cached from hasher, use the cached results,
        /// otherwise compute as normal.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row. </param>
        /// <returns>  True if the expressions are equal otherwise false. </returns>
        public override bool Equals(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            // Check if used variables in expression are same
            if (TableResults.RowProxy.AreIdenticalVars(in x, in y, this.usedVars)) return true;

            if (this.boundHasher == null) return this.EqualsNotCached(in x, in y);
            else return this.EqualsCached(in x, in y);
        }

        private bool EqualsCached(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expr.TryEvaluate(x, out T xValue);
            if (this.lastYRow != y.index)
            {
                this.lastYSuccess = this.expr.TryEvaluate(y, out this.lastYValue);
                this.lastYRow = y.index;
            }
            return Equals(xSuccess, this.lastYSuccess, xValue, this.lastYValue);
        }

        private bool EqualsNotCached(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expr.TryEvaluate(in x, out T xValue);
            var ySuccess = this.expr.TryEvaluate(in y, out T yValue);
            return Equals(xSuccess, ySuccess, xValue, yValue);
        }

        private bool Equals(bool xSuccess, bool ySuccess, T xValue, T yValue)
        {
            if (!ySuccess && !xSuccess) return true;
            else if (!ySuccess && xSuccess) return false;
            else if (ySuccess && !xSuccess) return false;
            else return EqualsValues(xValue, yValue);
        }

        protected abstract bool EqualsValues(T xValue, T yValue);

        public override void SetCache(ExpressionHasher cache)
        {
            this.boundHasher = cache;
        }
    }

    internal class ExpressionIntegerEqualityComparer : ExpressionEqualityComparer<int>
    {
        public ExpressionIntegerEqualityComparer(ExpressionHolder expressionHolder) : base(expressionHolder)
        {}

        protected override bool EqualsValues(int xValue, int yValue)
        {
            return xValue == yValue;
        }

        public override ExpressionEqualityComparer Clone()
        {
            return new ExpressionIntegerEqualityComparer(this.expressionHolder);
        }
    }

    internal class ExpressionStringEqualityComparer : ExpressionEqualityComparer<string>
    {
        public ExpressionStringEqualityComparer(ExpressionHolder expressionHolder) : base(expressionHolder)
        { }

        protected override bool EqualsValues(string xValue, string yValue)
        {
            return xValue == yValue;
        }

        public override ExpressionEqualityComparer Clone()
        {
            return new ExpressionStringEqualityComparer(this.expressionHolder);
        }
    }
}
