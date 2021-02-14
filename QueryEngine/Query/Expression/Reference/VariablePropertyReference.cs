using System;

namespace QueryEngine
{
    /// <summary>
    /// Property reference of an element.
    /// </summary>
    /// <typeparam name="T"> Type of property referenced. </typeparam>
    internal sealed class VariablePropertyReference<T> : VariableReference<T>
    {
        private int PropertyID { get; }
        /// <summary>
        /// Creates a property reference based on index of an element from a result and an accessed
        /// property.
        /// </summary>
        /// <param name="nHolder"> Holder of string representation of the name. </param>
        /// <param name="varIndex"> Index in a result during evaluation. </param>
        /// <param name="propID"> ID of the accessed property. </param>
        public VariablePropertyReference(VariableReferenceNameHolder nHolder, int varIndex, int propID) : base(nHolder, varIndex)
        {
            if (propID < 0)
                throw new ArgumentException($"{this.GetType()}, property ID must be >= 0, propID == {propID}.");

            this.PropertyID = propID;
        }

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
            Element element = elements[this.VariableIndex];
            return element.Table.TryGetPropertyValue(element.ID, this.PropertyID, out returnValue);
        }

        public override bool TryEvaluate(in Element[] elements, out T returnValue)
        {
            Element element = elements[this.VariableIndex];
            return element.Table.TryGetPropertyValue(element.ID, this.PropertyID, out returnValue);
        }

        public override bool TryEvaluate(in GroupByResultsList.GroupProxyList group, out T returnValue)
        {
            Element element = group.groupRepresentant[this.VariableIndex];
            return element.Table.TryGetPropertyValue(element.ID, this.PropertyID, out returnValue);
        }

        public override bool TryEvaluate(in GroupByResultsBucket.GroupProxyBucket group, out T returnValue)
        {
            Element element = group.groupRepresentant[this.VariableIndex];
            return element.Table.TryGetPropertyValue(element.ID, this.PropertyID, out returnValue);
        }

        public override bool TryEvaluate(in GroupByResultsArray.GroupProxyArray group, out T returnValue)
        {
            Element element = group.groupRepresentant[this.VariableIndex];
            return element.Table.TryGetPropertyValue(element.ID, this.PropertyID, out returnValue);
        }

        public override bool TryEvaluate(in AggregateBucketResult[] group, out T returnValue)
        {
            return ((AggregateBucketResultStreamed<T>)group[this.ExprPosition]).GetValue(out returnValue);
        }

        public override bool ContainsAggregate()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (obj.GetType() != this.GetType()) return false;
            else
            {
                var tmp = (VariablePropertyReference<T>)obj;
                if (tmp.VariableIndex == this.VariableIndex && tmp.PropertyID == this.PropertyID) return true;
                else return false;
            }
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException($"{this.GetType()}, calling not impl. function.");
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
        /// <param name="propID"> ID of the accessed property.</param>
        /// <returns> Property reference node. </returns>
        public static ExpressionBase Create(VariableReferenceNameHolder nameHolder, int varIndex, Type type, int propID)
        {
            if (type == typeof(string))
                return new VariablePropertyReference<string>(nameHolder, varIndex, propID);
            else if (type == typeof(int))
                return new VariablePropertyReference<int>(nameHolder, varIndex, propID);
            else throw new ArgumentException($"VariableReferenceFactory, inputed wrong type of property.");
        }
    }
}
