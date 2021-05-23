using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A base class for a variable reference.
    /// </summary>
    /// <typeparam name="T"> A type of the return value. </typeparam>
    internal abstract class VariableReference<T> : ExpressionReturnValue<T>
    {
        /// <summary>
        /// Stores information about the name of the variable reference.
        /// </summary>
        protected VariableReferenceNameHolder NameHolder { get; }

        /// <summary>
        /// An index of a variable to be evaluated with a given result.
        /// </summary>
        protected int VariableIndex { get; }

        /// <summary>
        /// Creates a variable reference.
        /// </summary>
        /// <param name="nHolder"> A holder of string representation of the name. </param>
        /// <param name="varIndex"> An index of an element in a result during evaluation.</param>
        protected VariableReference(VariableReferenceNameHolder nHolder, int varIndex)
        {
            if (nHolder == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a construtor.");
            else if (varIndex < 0)
                throw new ArgumentException($"{this.GetType()}, variable index must be >= 0, index == {varIndex}.");

            this.NameHolder = nHolder;
            this.VariableIndex = varIndex;
        }

        /// <summary>
        /// Returns a List of used variable references in the expression node.
        /// If the variable is already inside the List, the variable is not included.
        /// </summary>
        /// <param name="vars"> A List of already collected variables. </param>
        /// <returns> A List of collected variables, the same List as the one in func parameters.</returns>
        public override void CollectUsedVars(ref List<int> vars)
        {
            if (!vars.Contains(this.VariableIndex)) vars.Add(this.VariableIndex);
        }

        public override string ToString()
        {
            return NameHolder.Name + (NameHolder.PropName != null ? ("." + NameHolder.PropName) : "");
        }
    }
}
