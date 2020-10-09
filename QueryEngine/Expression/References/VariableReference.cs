/*! \file 
This file includes definitions of variable reference expression.
These are the references to a variable or a property of a variable or an id of a variable.
These expressions will evaluate based on a given element result.
The references contain holder that includes information what part of a result they refer to.
This file also includes a factory for references stated above.
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

        /// <summary>
        /// Returns a list of used variable references in the expression node.
        /// If the variable is already inside the list, the variable is not included.
        /// </summary>
        /// <param name="vars"> A list of already collected variables. </param>
        /// <returns> A list of collected variables, the same list as the one in func parameters.</returns>
        public override List<int> CollectUsedVars(List<int> vars)
        {
            if (!vars.Contains(this.VariableIndex)) vars.Add(this.VariableIndex);
            return vars;
        }

        public override string ToString()
        {
            return NameHolder.Name + (NameHolder.PropName != null ? ("." + NameHolder.PropName) : "");
        }

    }
  



}
