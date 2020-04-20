/*! \file
 
    This file includes base class for all expression nodes.

    Each node has evaluation method that tries to evalue the expression.
    If the evaluation fails (missing property value on element) it returns false.
    If it returns true, the value can be returned by casting the node to appropriate return value node.

*/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for every expression node.
    /// </summary>
    abstract class ExpressionBase
    {
        /// <summary>
        /// Stores inforamtion whether the expression node was successfully evaluated.
        /// If set to true, the get value method from expression return value must be set to a default value
        /// of the templated type.
        /// If set to true, the get value as string method should return "null".
        /// </summary>
        public bool IsNull {get; protected set;}

        /// <summary>
        /// Evaluates expression.
        /// </summary>
        /// <param name="elements"> One result from a match query.</param>
        /// <returns> True if successfully evaluated or false.</returns>
        public abstract bool TryEvaluate(Element[] elements);

        /// <summary>
        /// Returns string value of the value. If the IsNull is set to true, it should return a "null".
        /// </summary>
        /// <returns> String representaition of the value. </returns>
        public abstract string GetValueAsString();
    }

    /// <summary>
    /// Node that specifies holder during evaluation of expression.
    /// Each expression node will implement this interface.
    /// </summary>
    /// <typeparam name="T"> Type of return value.</typeparam>
    abstract class ExpressionReturnValue<T> : ExpressionBase
    {
        protected T Value;

        public T GetValue()
        {
            return this.Value;
        }

        /// <summary>
        /// Returns string value of the value. If the IsNull is set to true, it should return a "null".
        /// </summary>
        /// <returns> String representaition of the value. </returns>
        public override string GetValueAsString()
        {
            return (!(this.IsNull) ? "Null" : this.Value.ToString());
        }
    }

    /// <summary>
    /// Class holds entire expression.
    /// Optionally label representing entire expression.
    /// </summary>
    sealed class ExpressionHolder
    {
        public string Label { get; private set; } 
        public ExpressionBase Expr { get; private set; }

        /// <summary>
        /// Constructs expression holder.
        /// </summary>
        /// <param name="ex"> Expression base node. </param>
        /// <param name="label"> Label of the expression. </param>
        public ExpressionHolder(ExpressionBase ex, string label = null)
        {
            this.Expr = ex;
            this.Label = label;
        }

        /// <summary>
        /// Returns label representing entire expression or original expression as a string.
        /// </summary>
        public override string ToString()
        {
            return this.Label != null ? this.Label : this.Expr.ToString();
        }

    }

}
