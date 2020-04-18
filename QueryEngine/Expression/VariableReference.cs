/*! \file 
 
    This file includes definitions of variable references expression.
    This expressions will evaluate based on given element result.
    Templated properties or variable references are holders of property values.
 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for variable reference.
    /// </summary>
    /// <typeparam name="T"> Type of return value. </typeparam>
    abstract class VariableReference<T> : ExpressionReturnValueNode<T>
    {
        /// <summary>
        /// Index of a variable to be evaluated from a given result.
        /// </summary>
        protected int VariableIndex { get; private set; }

        /// <summary>
        /// Creates a variable reference.
        /// </summary>
        /// <param name="varIndex"> Index of an element in a result during evaluation.</param>
        public VariableReference(int varIndex) 
        {
            this.VariableIndex = varIndex;
        }

    }

    /// <summary>
    /// Property reference of an element.
    /// </summary>
    /// <typeparam name="T"> Type of property referenced. </typeparam>
    abstract class VariablePropertyReference<T> : VariableReference<T>
    {
        /// <summary>
        /// Name of a property that will be accessed during evaluation of an element.
        /// </summary>
        protected string PropertyName { get; private set; }

        /// <summary>
        /// Creates a property reference based on index of an element from a result and an accessed
        /// property.
        /// </summary>
        /// <param name="varIndex"> Index in a result during evaluation. </param>
        /// <param name="propName"> Name of an accessed property during evaluation. </param>
        public VariablePropertyReference(int varIndex, string propName) : base(varIndex)
        {
            this.PropertyName = propName;
        }

        /// <summary>
        /// Accesses property of an element based on variable index.
        /// Always sets value, because we expect that the out value is set on default if failed to evaluate.
        /// </summary>
        /// <param name="elements"> Result from a match query. </param>
        /// <returns> True on successful evaluation otherwise false. </returns>
        public override bool TryEvaluate(Element[] elements)
        {
            bool success = elements[this.VariableIndex].TryGetPropertyValue(this.PropertyName, out T retValue);
            this.Value = retValue;
            return success;
        }
    }

    /// <summary>
    /// Represents a reference to an element ID.
    /// </summary>
    class VariableIDReference : VariableReference<int>
    {
        public VariableIDReference(int varIndex) : base(varIndex) { }

        /// <summary>
        /// Accesses id of an element. This always succedes.
        /// </summary>
        /// <param name="elements"> Result from a match query. </param>
        /// <returns> True on successful evaluation otherwise false. </returns>
        public override bool TryEvaluate(Element[] elements)
        {
            this.Value = elements[this.VariableIndex].ID;
            return true;
        }
    }

}
