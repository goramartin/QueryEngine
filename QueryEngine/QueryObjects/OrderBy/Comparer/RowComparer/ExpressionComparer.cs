using System;
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
    internal abstract class ExpressionComparer : IRowComparer
    {
        protected readonly ExpressionHolder expressionHolder;
        protected readonly bool isAscending;
        protected readonly List<int> usedVars;
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
        /// <returns> Specialised comparer </returns>
        public static ExpressionComparer Factory(ExpressionHolder expressionHolder, bool ascending, Type typeOfExpression)
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
        ThreadLocal<T> value = new ThreadLocal<T>(() => default);
        ThreadLocal<bool> success = new ThreadLocal<bool>(() => false);
        ThreadLocal<int> lastRow = new ThreadLocal<int>(() => -1);

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
            //select x match (x) -> (e) -> (q) order by x.Prop;
            // Check if used variables in expression are same
            if (AreIdenticalVars(x, y)) return 0;

            if (x.index != lastRow.Value)
            {
                lastRow.Value = x.index;
                success.Value = this.expressionHolder.TryGetExpressionValue(x, out T val);
                value.Value = val;
            }
            
            var ySuccess = this.expressionHolder.TryGetExpressionValue(y, out T yValue);

            int retValue = 0;
            if (success.Value && !ySuccess) retValue = -1;
            else if (!success.Value && ySuccess) retValue = 1;
            else if (!success.Value && !ySuccess) retValue = 0;
            else retValue = this.CompareValues(value.Value, yValue);

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

        /// <summary>
        /// Checks whether used variables inside expression are same.
        /// In case there are the same, the expression should give the same 
        /// result.
        /// </summary>
        /// <param name="x"> First row. </param>
        /// <param name="y"> Second row.</param>
        /// <returns> True if all used variables are the same. </returns>
        private bool AreIdenticalVars(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            for (int i = 0; i < usedVars.Count; i++)
                if (x[i].ID != y[i].ID) return false;

            return true;
        }
        
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
