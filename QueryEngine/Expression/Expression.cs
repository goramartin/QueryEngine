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
    abstract class ExpressionBaseNode
    {
        /// <summary>
        /// Evaluates expression.
        /// </summary>
        /// <param name="elements"> One result from a match query.</param>
        /// <returns> True if successfully evaluated or false.</returns>
        public abstract bool TryEvaluate(Element[] elements);
        public abstract string GetValueAsString();

    }

    /// <summary>
    /// Node that specifies holder during evaluation of expression.
    /// Each expression node will implement this interface.
    /// </summary>
    /// <typeparam name="T"> Type of return value.</typeparam>
    abstract class ExpressionReturnValueNode<T> : ExpressionBaseNode
    {
        protected T Value;

        public T GetValue()
        {
            return this.Value;
        }

        public override string GetValueAsString()
        {
            return this.Value.ToString();
        }
    }



}
