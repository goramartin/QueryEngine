using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
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


}
