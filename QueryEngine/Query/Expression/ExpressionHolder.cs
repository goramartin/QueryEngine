using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A class holds an entire expression.
    /// </summary>
    internal sealed class ExpressionHolder : ExpressionBase
    {
        /// <summary>
        /// A string representation of the expression.
        /// If not set, then a recursive call of .ToString() on the expression node is made.
        /// </summary>
        private string Label { get; }
        public ExpressionBase Expr { get; }
        public Type ExpressionType { get; }

        /// <summary>
        /// Constructs an expression holder.
        /// </summary>
        /// <param name="ex"> An expression base node. </param>
        /// <param name="label"> A label of the expression. </param>
        public ExpressionHolder(ExpressionBase ex, string label = null)
        {
            if (ex == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

            this.Expr = ex;
            this.ExpressionType = ex.GetExpressionType();
            this.Label = label;
        }

        /// <summary>
        /// Returns a label representing the entire expression or the original expression as a string.
        /// </summary>
        public override string ToString()
        {
            return this.Label != null ? this.Label : this.Expr.ToString();
        }


        /// <summary>
        /// Returns a type of the containing expression.
        /// </summary>
        public override Type GetExpressionType()
        {
            return this.ExpressionType;
        }

        /// <summary>
        /// Returns a List of used variable references in the expression node.
        /// If the variable is already inside the List, the variable is not included.
        /// </summary>
        /// <param name="vars"> A List of already collected variables. </param>
        /// <returns> A List of collected variables, the same List as the one in func parameters.</returns>
        public override void CollectUsedVars(ref List<int> vars)
        {
            this.Expr.CollectUsedVars(ref vars);
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
