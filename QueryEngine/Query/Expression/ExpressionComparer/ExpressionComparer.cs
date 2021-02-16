using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Base class for expression value comparing.
    /// Each class contains an expression that will be evaluated with given rows.
    /// Then the values are compared with templated compare method.
    /// The child classes can cache the left argument in the compare function.
    /// It is because the same left argument is compared with multiple elements in a row, thus it can save a bit of computation.
    /// However, this is only done in the single threaded enviroment.
    /// Null values in ascending order appear as the first.
    /// </summary>
    internal abstract class ExpressionComparer : IExpressionComparer
    {
        protected ExpressionHolder expressionHolder;
        protected int[] usedVars;
        public readonly bool isAscending = true;
        public readonly bool cacheResults;
        
        /// <summary>
        /// Constructs expression comparer.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated during comparing. </param>
        /// <param name="ascending"> Whether to use asc or desc order. </param>
        /// <param name="cacheResults"> Whether to cache results of the computed expressions. </param>
        protected ExpressionComparer(ExpressionHolder expressionHolder, bool ascending, bool cacheResults)
        {
            if (expressionHolder == null)
                throw new ArgumentException($"{this.GetType()}, trying to assign null to a constructor.");

            this.expressionHolder = expressionHolder;
            this.isAscending = ascending;
            this.cacheResults = cacheResults;
            this.usedVars = expressionHolder.CollectUsedVars(new List<int>()).ToArray();
        }

        public abstract int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y);

        /// <summary>
        /// Expression comparer factory.
        /// Creates a templated expression comparers based on a given type.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated. </param>
        /// <param name="isAscending"> Whether to use ascending order or descending. </param>
        /// <param name="cacheResults"> Whether to cache results of the computed expressions. </param>
        /// <returns> Specialised comparer. </returns>
        public static ExpressionComparer Factory(ExpressionHolder expressionHolder, bool isAscending, bool cacheResults)
        {
            if (expressionHolder.ExpressionType == typeof(int))
                return new ExpressionIntegerComparer(expressionHolder, isAscending, cacheResults);
            else if (expressionHolder.ExpressionType == typeof(string))
                return new ExpressionStringComparer(expressionHolder, isAscending, cacheResults);
            else throw new ArgumentException($"Expression comparer factory, unknown type passed to a expression comparer factory.");
        }

        public abstract ExpressionComparer Clone(bool cacheResults);

        public ExpressionHolder GetExpressionHolder() => this.expressionHolder;
    }

    internal abstract class ExpressionComparer<T> : ExpressionComparer
    {
        private ExpressionReturnValue<T> expr;
        protected bool lastXSuccess = false;
        protected int lastXRow = -1;
        protected T lastXValue = default;

        protected bool lastYSuccess = false;
        protected int lastYRow = -1;
        protected T lastYValue = default;

        public ExpressionComparer(ExpressionHolder expressionHolder, bool ascending, bool cacheResults) : base(expressionHolder, ascending, cacheResults)
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
            if (TableResults.RowProxy.AreIdenticalVars(in x, in y, this.usedVars)) return 0;

            if (this.cacheResults) return CachedCompare(in x, in y);
            else return NonCachedCompare(in x, in y);
        }

        private int CachedCompare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            if (x.index != this.lastXRow)
            {
                this.lastXSuccess = this.expr.TryEvaluate(x, out this.lastXValue);
                this.lastXRow = x.index;
            }
            if (y.index != this.lastYRow)
            {
                this.lastYSuccess = this.expr.TryEvaluate(y, out this.lastYValue);
                this.lastYRow = y.index;
            }
            return this.Compare(this.lastXSuccess, this.lastYSuccess, this.lastXValue, this.lastYValue);
        }
        private int NonCachedCompare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expr.TryEvaluate(x, out T xValue);
            var ySuccess = this.expr.TryEvaluate(y, out T yValue);
            return this.Compare(xSuccess, ySuccess, xValue, yValue);
        }

        private int Compare(bool xSuccess, bool ySuccess, T xValue, T yValue)
        {
            int retValue = 0;
            if (xSuccess && !ySuccess) retValue = 1;
            else if (!xSuccess && ySuccess) retValue = -1;
            else if (!xSuccess && !ySuccess) retValue = 0;
            else retValue = this.CompareValues(xValue, yValue);

            if (!this.isAscending)
            {
                if (retValue == -1) retValue = 1;
                else if (retValue == 1) retValue = -1;
                else { /* retValue == 0 */}
            }
            return retValue;
        }

        protected abstract int CompareValues(T xValue, T yValue);

        public void SetYCache(bool ySuccess, int yRow, T yValue)
        {
            this.lastYSuccess = ySuccess;
            this.lastYRow = yRow;
            this.lastYValue = yValue;
        }
        public void SetXCache(bool xSuccess, int xRow, T xValue)
        {
            this.lastXSuccess = xSuccess;
            this.lastXRow = xRow;
            this.lastXValue = xValue;
        }
    }

    internal class ExpressionIntegerComparer : ExpressionComparer<int>
    {
        public ExpressionIntegerComparer(ExpressionHolder expressionHolder, bool ascending, bool cacheResults) : base(expressionHolder, ascending, cacheResults)
        {  }

        protected override int CompareValues(int xValue, int yValue)
        {
            return xValue.CompareTo(yValue);
        }

        public override ExpressionComparer Clone(bool cacheResults)
        {
            return new ExpressionIntegerComparer(this.expressionHolder, this.isAscending, cacheResults);
        }
    }

    internal class ExpressionStringComparer : ExpressionComparer<string>
    {
        public ExpressionStringComparer(ExpressionHolder expressionHolder, bool ascending, bool cacheResults) : base(expressionHolder, ascending, cacheResults)
        { }
        protected override int CompareValues(string xValue, string yValue)
        {
            return xValue.CompareTo(yValue);
        }

        public override ExpressionComparer Clone(bool cacheResults)
        {
            return new ExpressionStringComparer(this.expressionHolder, this.isAscending, cacheResults);
        }
    }
}
