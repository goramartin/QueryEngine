/*! \file 
 
    This file contains definitions of a row comparer and a expression comparer.
    
    Each row comparer contains a list of expression comparers.
    During row comparing each expression comparer compares values computed with the given row.
    Based on the result it decides whether to continue comparing expression or returns resulting value.
 
    Expression comparer is given to rows and computes expression value with the both rows. The values are then compared
    using templated compare methods. 

    Null values in descenging order appear as last elements.
 
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{

    /// <summary>
    /// Interface for comparing rows of a result table.
    /// </summary>
    interface IRowProxyComparer
    {
        int Compare(in Results.RowProxy x, in Results.RowProxy y);
    }

    /// <summary>
    /// Compares two rows.
    /// Contains list of all expression to compared with the rows.
    /// </summary>
    class RowComparer : IRowProxyComparer
    {
        private List<IRowProxyComparer> comparers { get; }

        /// <summary>
        /// Creates a row comparer.
        /// </summary>
        /// <param name="rowProxyComparers"> Expected a list of expression comparers.</param>
        public RowComparer(List<IRowProxyComparer> rowProxyComparers)
        {
            this.comparers = rowProxyComparers;
        }

        /// <summary>
        /// Compares rows for every expression.
        /// If it find value !=0 then it will retrun the value. Otherwise it will continue comparing.
        /// </summary>
        /// <param name="x"> First row.</param>
        /// <param name="y"> Second row. </param>
        /// <returns> Less than zero x precedes y in the sort order.
        /// Zero x occurs in the same position as y in the sort order.
        /// Greater than zero x follows y in the sort order.</returns>
        public int Compare(in Results.RowProxy x, in Results.RowProxy y)
        {
            int result = 0;
            for (int i = 0; i < this.comparers.Count; i++)
            {
                result = this.comparers[i].Compare(x, y);
                if (result != 0) return result;
            }
            return result;
        }
    }


    /// <summary>
    /// Base class for expression value comparing.
    /// Each class contains an expression that will be evaluated with given rows.
    /// Then the values are compared with templated compare method.
    /// </summary>
    abstract class ExpressionComparer : IRowProxyComparer
    {
        protected ExpressionHolder expressionHolder { get; }
        protected bool Ascending { get; }

        /// <summary>
        /// Constructs expression comparer.
        /// </summary>
        /// <param name="expressionHolder"> Expression to be evaluated during comparing. </param>
        /// <param name="ascending"> Whether to use asc or desc order. </param>
        protected ExpressionComparer(ExpressionHolder expressionHolder, bool ascending)
        {
            this.expressionHolder = expressionHolder;
            this.Ascending = ascending;
        }

        public abstract int Compare(in Results.RowProxy x, in Results.RowProxy y);

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
    abstract class ExpressionComparer<T> : ExpressionComparer
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
        public override int Compare(in Results.RowProxy x, in Results.RowProxy y)
        {
            var xSuccess = expressionHolder.TryGetExpressionValue(x, out T xValue);
            var ySuccess = expressionHolder.TryGetExpressionValue(y, out T yValue);

            int retValue = 0;
            if (xSuccess && !ySuccess) retValue = -1;
            else if (!xSuccess && ySuccess) retValue = 1;
            else if (!xSuccess && !ySuccess) retValue = 0;
            else retValue = this.CompareValues(xValue, yValue);

            if (!Ascending) 
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

    class ExpressionIntegerCompaper : ExpressionComparer<int>
    {

        public ExpressionIntegerCompaper(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        { }

        protected override int CompareValues(int x, int y)
        {
            return x.CompareTo(y);
        }
    }

    class ExpressionStringCompaper : ExpressionComparer<string>
    {
        public ExpressionStringCompaper(ExpressionHolder expressionHolder, bool ascending = true) : base(expressionHolder, ascending) 
        { }

        protected override int CompareValues(string x, string y)
        {
            return x.CompareTo(y);
        }
    }

}
