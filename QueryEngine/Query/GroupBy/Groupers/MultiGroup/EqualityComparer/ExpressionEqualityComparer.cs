﻿using System;
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
        protected ExpressionHolder expressionHolder;
        protected List<int> usedVars;
        protected ExpressionHasher boundHasher;
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
        public abstract void SetCache(ExpressionHasher cache);
    }

    internal abstract class ExpressionEqualityComparer<T> : ExpressionEqualityComparer
    {
        ExpressionReturnValue<T> expr;
        public T resultLastY = default;
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
            if (AreIdenticalVars(x, y)) return true;

            if (this.boundHasher == null) return this.EqualsNotCached(in x, in y);
            else return this.EqualsCached(in x, in y);
        }

        private bool EqualsCached(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expr.TryEvaluate(x, out T xValue);
            if (this.rowlastY != y.index)
            {
                this.successLastY = this.expr.TryEvaluate(y, out this.resultLastY);
                rowlastY = y.index;
            }

            if (!this.successLastY && !xSuccess) return true;
            else return Compare(xValue, resultLastY);
        }

        private bool EqualsNotCached(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expr.TryEvaluate(in x, out T xValue);
            var ySuccess = this.expr.TryEvaluate(in y, out T yValue);

            if (xSuccess && !ySuccess) return true;
            else return Compare(xValue, yValue);
        }

        protected abstract bool Compare(T x, T y);

        public override void SetCache(ExpressionHasher cache)
        {
            this.boundHasher = cache;
        }
    }

    internal class ExpressionIntegerEqualityComparer : ExpressionEqualityComparer<int>
    {
        public ExpressionIntegerEqualityComparer(ExpressionHolder expressionHolder) : base(expressionHolder)
        {}

        protected override bool Compare(int x, int y)
        {
            return x == y;
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

        protected override bool Compare(string x, string y)
        {
            return x == y;
        }

        public override ExpressionEqualityComparer Clone()
        {
            return new ExpressionStringEqualityComparer(this.expressionHolder);
        }
    }
}
