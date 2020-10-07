using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for expression value comparing.
    /// Each class contains an expression that will be evaluated with given rows.
    /// Then the values are compared with templated compare method.
    /// </summary>
    internal abstract class ExpressionComparer : IRowComparer
    {
        protected readonly ExpressionHolder expressionHolder;
        protected readonly bool isAscending;

        /// <summary>
        /// Constructs expression comparer.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated during comparing. </param>
        /// <param name="ascending"> Whether to use asc or desc order. </param>
        protected ExpressionComparer(ExpressionHolder expressionHolder, bool ascending)
        {
            this.expressionHolder = expressionHolder;
            this.isAscending = ascending;
        }

        public abstract int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y);

        /// <summary>
        /// Expression comparer facotry.
        /// Creates a templated expression comparers based on a given type.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated. </param>
        /// <param name="ascending"> Whether to use ascending order or descending. </param>
        /// <param name="typeOfExpression"> Type of comparer. </param>
        /// <returns> Specialised comparer </returns>
        public static ExpressionComparer ExpressionCompaperFactory(ExpressionHolder expressionHolder, bool ascending, Type typeOfExpression)
        {
            if (typeOfExpression == typeof(int))
                return new ExpressionIntegerCompaper(expressionHolder, ascending);
            else if (typeOfExpression == typeof(string))
                return new ExpressionStringCompaper(expressionHolder, ascending);
            else throw new ArgumentException($"ExpressionComparer, unknown type passed to a expression comparer factory.");
        }
    }

    /// <summary>
    /// Base class for specialised comparers.
    /// </summary>
    /// <typeparam name="T"> Type of expression return value that will be evaluated. </typeparam>
    internal abstract class ExpressionComparer<T> : ExpressionComparer
    {
        protected ExpressionComparer(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        { }

        /// <summary>
        /// Tries to evaluate containing expression with given rows.
        /// Values are compared always in ascending order and switched to descending order if neccessary.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row. </param>
        /// <returns> Less than zero x precedes y in the sort order.
        /// Zero x occurs in the same position as y in the sort order.
        /// Greater than zero x follows y in the sort order.</returns>
        public override int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            var xSuccess = this.expressionHolder.TryGetExpressionValue(x, out T xValue);
            var ySuccess = this.expressionHolder.TryGetExpressionValue(y, out T yValue);

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

        /// <summary>
        /// Compares specialised types. Always returns ascending order.
        /// </summary>
        /// <param name="x"> First value. </param>
        /// <param name="y"> Second value. </param>
        /// <returns> Less than zero x precedes y in the sort order.
        /// Zero x occurs in the same position as y in the sort order.
        /// Greater than zero x follows y in the sort order.</returns>
        protected abstract int CompareValues(T x, T y);
    }

    internal sealed class ExpressionIntegerCompaper : ExpressionComparer<int>
    {

        public ExpressionIntegerCompaper(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        { }

        protected override int CompareValues(int x, int y)
        {
            return x.CompareTo(y);
        }
    }

    internal sealed class ExpressionStringCompaper : ExpressionComparer<string>
    {
        public ExpressionStringCompaper(ExpressionHolder expressionHolder, bool ascending = true) : base(expressionHolder, ascending)
        { }

        protected override int CompareValues(string x, string y)
        {
            return x.CompareTo(y);
        }
    }
}
