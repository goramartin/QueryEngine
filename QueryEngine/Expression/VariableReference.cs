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
    internal abstract class VariableReference<T> : ExpressionReturnValue<T>
    {
        /// <summary>
        /// Stores information about the name of the variable reference.
        /// </summary>
        protected VariableReferenceNameHolder NameHolder { get; }

        /// <summary>
        /// Index of a variable to be evaluated from a given result.
        /// </summary>
        protected int VariableIndex { get; }

        /// <summary>
        /// Creates a variable reference.
        /// </summary>
        /// <param name="nHolder"> Holder of string representation of the name. </param>
        /// <param name="varIndex"> Index of an element in a result during evaluation.</param>
        protected VariableReference(VariableReferenceNameHolder nHolder, int varIndex) 
        {
            this.NameHolder = nHolder;
            this.VariableIndex = varIndex;
        }

        public override string ToString()
        {
            return NameHolder.Name + (NameHolder.PropName != null ? ("." + NameHolder.PropName) : "");
        }

    }

    /// <summary>
    /// Property reference of an element.
    /// </summary>
    /// <typeparam name="T"> Type of property referenced. </typeparam>
    internal sealed class VariablePropertyReference<T> : VariableReference<T>
    {
        /// <summary>
        /// Creates a property reference based on index of an element from a result and an accessed
        /// property.
        /// </summary>
        /// <param name="nHolder"> Holder of string representation of the name. </param>
        /// <param name="varIndex"> Index in a result during evaluation. </param>
        public VariablePropertyReference(VariableReferenceNameHolder nHolder, int varIndex) : base(nHolder, varIndex)
        { }

        /// <summary>
        /// Returns type of this expression node.
        /// </summary>
        public override Type GetExpressionType()
        {
            return typeof(T);
        }

        /// <summary>
        /// Accesses property of an element based on variable index.
        /// Always sets value, because we expect that the out value is set on default if failed to evaluate.
        /// </summary>
        /// <param name="elements"> Result from a match query. </param>
        /// <param name="returnValue">Return value of this expression node. </param>
        /// <returns> True on successful evaluation otherwise false. </returns>
        public override bool TryEvaluate(in TableResults.RowProxy elements, out T returnValue)
        {
             return elements[this.VariableIndex].TryGetPropertyValue(this.NameHolder.PropName, out returnValue);
        }
    }

    /// <summary>
    /// Represents a reference to an element ID.
    /// </summary>
    internal sealed class VariableIDReference : VariableReference<int>
    {
        /// <summary>
        /// Constructs id reference.
        /// </summary>
        /// <param name="nHolder">Holder of string representation of the name.</param>
        /// <param name="varIndex"> Index in a result during evaluation.</param>
        public VariableIDReference(VariableReferenceNameHolder nHolder, int varIndex) : base(nHolder, varIndex) { }

        /// <summary>
        /// Returns type of this expression.
        /// </summary>
        public override Type GetExpressionType()
        {
            return typeof(int);
        }

        /// <summary>
        /// Accesses id of an element. This always succedes.
        /// </summary>
        /// <param name="elements"> Result from a match query. </param>
        /// <param name="returnValue">Return value of this expression node. </param>
        /// <returns> True on successful evaluation otherwise false. </returns>
        public override bool TryEvaluate(in TableResults.RowProxy elements, out int returnValue)
        {
            returnValue = elements[this.VariableIndex].ID;
            return true;
        }
    }



    /// <summary>
    /// Class is used as a holder for printing directives.
    /// When select initialises printing the class will serve as a base information for printing headers.
    /// </summary>
    internal sealed class VariableReferenceNameHolder
    {
        /// <summary>
        /// Name of variable.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Property access to a variable.
        /// </summary>
        public string PropName { get; private set; }


        public VariableReferenceNameHolder(string name = null, string propName = null)
        {
            this.Name = name;
            this.PropName = propName;
        }

        /// <summary>
        /// Tries to set a name, will set if name is set to null.
        /// </summary>
        public bool TrySetName(string n)
        {
            if (this.Name == null) { this.Name = n; return true; }
            else return false;
        }
        /// <summary>
        /// Tries to set a property name, will set if property is set to null.
        /// </summary>
        public bool TrySetPropName(string n)
        {
            if (this.PropName == null) { this.PropName = n; return true; }
            else return false;
        }

        /// <summary>
        /// Check is the variable has no set contents.
        /// </summary>
        public bool IsEmpty()
        {
            if ((this.Name == null) && (this.PropName == null)) return true;
            else return false;
        }
    }

    /// <summary>
    /// Factory for templated property reference.
    /// </summary>
    internal static class VariableReferencePropertyFactory
    {
        /// <summary>
        /// Creates a typed property reference.
        /// </summary>
        /// <param name="nameHolder"> Name of the variable refernece. </param>
        /// <param name="varIndex"> Index of an accessed varible.</param>
        /// <param name="type"> Type of accessed property. </param>
        /// <returns> Property reference node. </returns>
        public static ExpressionBase Create(VariableReferenceNameHolder nameHolder, int varIndex, Type type)
        {
             if (type == typeof(string))
                return new VariablePropertyReference<string>(nameHolder, varIndex);
            else if (type == typeof(int))
                return new VariablePropertyReference<int>(nameHolder, varIndex);
            else throw new ArgumentException($"VariableReferenceFactory, inputed wrong type of property.");
        }
    }



}
