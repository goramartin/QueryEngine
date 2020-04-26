/*! \file
 
    This file includes base class for all expression nodes.

    Each node has evaluation method that tries to evalue the expression.
    If the evaluation fails (missing property value on element) it returns false.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for every expression node.
    /// Serves only as a holder.
    /// </summary>
    abstract class ExpressionBase
    {
        /// <summary>
        /// Gets expression type.
        /// </summary>
        /// <returns> Type of expression. </returns>
        public abstract Type GetExpressionType();
    }

    /// <summary>
    /// Node that specifies holder during evaluation of expression.
    /// Each expression node will implement this interface.
    /// </summary>
    /// <typeparam name="T"> Type of return value.</typeparam>
    abstract class ExpressionReturnValue<T> : ExpressionBase
    {
        /// <summary>
        /// Evaluates expression node and returns value inside value parameter.
        /// </summary>
        /// <param name="elements"> One results of a search. </param>
        /// <param name="value"> Value of the expression. </param>
        /// <returns> Bool on successful evaluation otherwise fasel. On success, the value parameter
        /// will contain value of the epression otherwise the value is undefined. </returns>
        public abstract bool TryEvaluate(in RowProxy elements, out T value);    
    }

    /// <summary>
    /// Class holds entire expression.
    /// Optionally label representing entire expression.
    /// </summary>
    sealed class ExpressionHolder : ExpressionBase
    {
        private string Label { get; }
        private ExpressionBase Expr { get; }
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
        public bool TryGetExpressionValue<T>(in RowProxy elements, out T returnValue)
        {
                if (((ExpressionReturnValue<T>)(this.Expr)).TryEvaluate(elements, out returnValue)) return true;
                else return false;
        }

        /// <summary>
        /// Returns type of containing expression.
        /// </summary>
        public override Type GetExpressionType()
        {
            return this.Expr.GetExpressionType();
        }
    }

}
