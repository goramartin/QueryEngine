/*! \file
This file includes definition of an expression holder.

Expression holder serves as a wrapper around expression tree.
It adds api that helps evaluation the expression.
*/

using System;
using System.Collections.Generic;

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
            if (ex == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

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

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (this.GetType() != obj.GetType()) return false;
            else
            {
                var tmp = (ExpressionHolder)obj;
                if (this.Expr.Equals(tmp.Expr)) return true;
                else return false;
            }
        }

        public override void SetExprPosition(int exprPos)
        {
            this.ExprPosition = exprPos;
            this.Expr.SetExprPosition(exprPos);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException($"{this.GetType()}, should never be called.");
        }
    }
}
