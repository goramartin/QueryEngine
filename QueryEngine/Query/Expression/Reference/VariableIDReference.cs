using System;

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
        public VariableIDReference(VariableReferenceNameHolder nHolder, int varIndex) : base(nHolder, varIndex) 
        { }

        public override bool ContainsAggregate()
        {
            return false;
        }

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

        public override bool TryEvaluate(in Element[] elements, out int returnValue)
        {
            returnValue = elements[this.VariableIndex].ID;
            return true;
        }

        public override bool TryEvaluate(in GroupByResultsList.GroupProxyList group, out int returnValue)
        {
            returnValue = group.groupRepresentant[this.VariableIndex].ID;
            return true;
        }

        public override bool TryEvaluate(in GroupByResultsBucket.GroupProxyBucket group, out int returnValue)
        {
            returnValue = group.groupRepresentant[this.VariableIndex].ID;
            return true;
        }

        public override bool TryEvaluate(in GroupByResultsArray.GroupProxyArray group, out int returnValue)
        {
            returnValue = group.groupRepresentant[this.VariableIndex].ID;
            return true;
        }

        public override bool TryEvaluate(in AggregateBucketResult[] group, out int returnValue)
        {
            return ((AggregateBucketResultStreamed<int>)group[this.ExprPosition]).GetValue(out returnValue);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (obj.GetType() != this.GetType()) return false;
            else
            {
                var tmp = (VariableIDReference)obj;
                if (tmp.VariableIndex == this.VariableIndex) return true;
                else return false;
            }
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException($"{this.GetType()}, calling not impl. function.");
        }
    }


}
