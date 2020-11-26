﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// Base class for expression value comparing.
    /// Each class contains an expression that will be evaluated with given rows.
    /// Then the values are compared with templated compare method.
    /// </summary>
    internal abstract class ExpressionComparer : IExpressionComparer
    {
        protected readonly ExpressionHolder expressionHolder;
        protected readonly bool isAscending;
        protected readonly List<int> usedVars;
        protected bool cacheResults;
        /// <summary>
        /// Constructs expression comparer.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated during comparing. </param>
        /// <param name="ascending"> Whether to use asc or desc order. </param>
        protected ExpressionComparer(ExpressionHolder expressionHolder, bool ascending)
        {
            this.expressionHolder = expressionHolder;
            this.isAscending = ascending;
            this.usedVars = expressionHolder.CollectUsedVars(new List<int>());
        }

        public abstract int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y);

        /// <summary>
        /// Expression comparer facotry.
        /// Creates a templated expression comparers based on a given type.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated. </param>
        /// <param name="ascending"> Whether to use ascending order or descending. </param>
        /// <param name="typeOfExpression"> Type of comparer. </param>
        /// <returns> Specialised comparer. </returns>
        public static ExpressionComparer Factory(ExpressionHolder expressionHolder, bool ascending, Type typeOfExpression)
        {
            if (typeOfExpression == typeof(int))
                return new ExpressionIntegerComparer(expressionHolder, ascending);
            else if (typeOfExpression == typeof(string))
                return new ExpressionStringComparer(expressionHolder, ascending);
            else throw new ArgumentException($"Expression comparer factory, unknown type passed to a expression comparer factory.");
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

        public void SetCachingResults(bool setValue)
        {
            this.cacheResults = setValue;
        }

    }

    internal abstract class ExpressionComparer<T> : ExpressionComparer
    {
        protected bool lastSuccess = false;
        protected int lastRow = -1;
        protected T lastXValue = default;
        ExpressionReturnValue<T> expr;

        public ExpressionComparer(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        /// <summary>
        /// Tries to evaluate containing expression with given rows.
        /// Values are compared always in ascending order and switched to descending order if neccessary.
        /// The cached campare is called only if the computation is not done in parallel.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row. </param>
        /// <returns> Less than zero x precedes y in the sort order.
        /// Zero x occurs in the same position as y in the sort order.
        /// Greater than zero x follows y in the sort order.</returns>
        public override int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            if (this.cacheResults) return CachedCompare(in x, in y);
            else return NonCachedCompare(in x, in y);
        }

        protected int CachedCompare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            if (x.index != this.lastRow)
            {
                this.lastSuccess = this.expr.TryEvaluate(x, out this.lastXValue);
                this.lastRow = x.index;
            }
            var ySuccess = this.expr.TryEvaluate(y, out T yValue);

            int retValue = 0;
            if (this.lastSuccess && !ySuccess) retValue = -1;
            else if (!this.lastSuccess && ySuccess) retValue = 1;
            else if (!this.lastSuccess && !ySuccess) retValue = 0;
            else retValue = this.CompareValues(this.lastXValue, yValue);

            if (!this.isAscending)
            {
                if (retValue == -1) retValue = 1;
                else if (retValue == 1) retValue = -1;
                else { }
            }

            return retValue;
        }
        protected int NonCachedCompare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expr.TryEvaluate(x, out T xValue);
            var ySuccess = this.expr.TryEvaluate(y, out T yValue);

            int retValue = 0;
            if (xSuccess && !ySuccess) retValue = -1;
            else if (!xSuccess && ySuccess) retValue = 1;
            else if (!xSuccess && !ySuccess) retValue = 0;
            else retValue = this.CompareValues(xValue, yValue);

            if (!this.isAscending)
            {
                if (retValue == -1) retValue = 1;
                else if (retValue == 1) retValue = -1;
                else { }
            }

            return retValue;
        }

        protected abstract int CompareValues(T xValue, T yValue);
    }


    internal class ExpressionIntegerComparer : ExpressionComparer<int>
    {
        public ExpressionIntegerComparer(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        {  }

        protected override int CompareValues(int xValue, int yValue)
        {
            return xValue.CompareTo(yValue);
        }
    }

    internal class ExpressionStringComparer : ExpressionComparer<string>
    {
        public ExpressionStringComparer(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        { }
        protected override int CompareValues(string xValue, string yValue)
        {
            return xValue.CompareTo(yValue);
        }

    }
}
