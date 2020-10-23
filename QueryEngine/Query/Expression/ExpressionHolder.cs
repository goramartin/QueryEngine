/*! \file
This file includes definition of an expression holder.

Expression holder serves as a wrapper around expression tree.
It adds api that helps evaluation the expression.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class holds entire expression.
    /// Optionally label representing entire expression.
    /// </summary>
    internal sealed class ExpressionHolder : ExpressionBase
    {
        private string Label { get; }
        public ExpressionBase Expr { get; }
        public Type ExpressionType { get; }

        /// <summary>
        /// Constructs expression holder.
        /// </summary>
        /// <param name="ex"> Expression base node. </param>
        /// <param name="label"> Label of the expression. </param>
        public ExpressionHolder(ExpressionBase ex, string label = null)
        {
            this.Expr = ex;
            this.ExpressionType = ex.GetExpressionType();
            this.Label = label;
        }

        /// <summary>
        /// Returns label representing entire expression or original expression as a string.
        /// </summary>
        public override string ToString()
        {
            return this.Label != null ? this.Label : this.Expr.ToString();
        }

        /// <summary>
        /// Tries evaluating expression with given element row.
        /// </summary>
        /// <typeparam name="T">Return value of the expression. </typeparam>
        /// <param name="elements">One results of the search.</param>
        /// <param name="returnValue"> Place to store return value of the expression. </param>
        /// <returns>True of successful evaluation otherwise false.</returns>
        public bool TryGetExpressionValue<T>(in TableResults.RowProxy elements, out T returnValue)
        {
            if (((ExpressionReturnValue<T>)(this.Expr)).TryEvaluate(elements, out returnValue)) return true;
            else return false;
        }

        /// <summary>
        /// Returns type of containing expression.
        /// </summary>
        public override Type GetExpressionType()
        {
            return this.ExpressionType;
        }


        /// <summary>
        /// Returns a list of used variable references in the expression node.
        /// If the variable is already inside the list, the variable is not included.
        /// </summary>
        /// <param name="vars"> A list of already collected variables. </param>
        /// <returns> A list of collected variables, the same list as the one in func parameters.</returns>
        public override List<int> CollectUsedVars(List<int> vars)
        {
            return this.Expr.CollectUsedVars(vars);
        }

        public override bool ContainsAggregate()
        {
            return this.Expr.ContainsAggregate();
        }

    }
}
