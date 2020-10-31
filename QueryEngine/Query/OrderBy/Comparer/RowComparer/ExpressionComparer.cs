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
    }

    internal class ExpressionIntegerComparer : ExpressionComparer
    {
        // To avoid casting every time Holder.TryGetValue()
        ExpressionReturnValue<int> expr;
        public ExpressionIntegerComparer(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        {
            this.expr = (ExpressionReturnValue<int>)expressionHolder.Expr;
        }

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
            // Check if used variables in expression are same
            if (AreIdenticalVars(x, y)) return 0;

            var xSuccess = this.expr.TryEvaluate(x, out int xValue);
            var ySuccess = this.expr.TryEvaluate(y, out int yValue);

            int retValue = 0;
            if (xSuccess && !ySuccess) retValue = -1;
            else if (!xSuccess && ySuccess) retValue = 1;
            else if (!xSuccess && !ySuccess) retValue = 0;
            else retValue = xValue.CompareTo(yValue);

            if (!this.isAscending)
            {
                if (retValue == -1) retValue = 1;
                else if (retValue == 1) retValue = -1;
                else { }
            }

            return retValue;
        }
    }

    internal class ExpressionStringComparer : ExpressionComparer
    {
        // To avoid casting every time Holder.TryGetValue()
        ExpressionReturnValue<string> expr;
        public ExpressionStringComparer(ExpressionHolder expressionHolder, bool ascending) : base(expressionHolder, ascending)
        {
            this.expr = (ExpressionReturnValue<string>)expressionHolder.Expr;
        }

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
            // Check if used variables in expression are same
            if (AreIdenticalVars(x, y)) return 0;

            var xSuccess = this.expr.TryEvaluate(x, out string xValue);
            var ySuccess = this.expr.TryEvaluate(y, out string yValue);

            int retValue = 0;
            if (xSuccess && !ySuccess) retValue = -1;
            else if (!xSuccess && ySuccess) retValue = 1;
            else if (!xSuccess && !ySuccess) retValue = 0;
            else retValue = xValue.CompareTo(yValue);

            if (!this.isAscending)
            {
                if (retValue == -1) retValue = 1;
                else if (retValue == 1) retValue = -1;
                else { }
            }

            return retValue;
        }
    }
}
