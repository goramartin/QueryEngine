/*! \file
This file includes a definition of a base class for a property that is visible to the Table class.
Properties are enclosed in the Table class.
Each property contains the name of the property defined in the json input schema.

Specialisations are generics and contain a List of values of the properties.
The base non generic property must be used to create a List of properties in the table,
because the types of properties are unknown before runtime.
 */

using System;

namespace QueryEngine
{
    /// <summary>
    /// An property in the Labeled-property model.
    /// Its functions are visible to a table.
    /// </summary>
    public abstract class Property
    {
        /// <summary>
        /// An identifier of the property.
        /// The identifiers are used globally in the entire graph.
        /// Thus if two properties have the same IRI, they are also of the same type.
        /// </summary>
        public string IRI { get; protected set; }

        /// <summary>
        /// Constructs an empty Property.
        /// </summary>
        public Property() { this.IRI = null; }

        /// <summary>
        /// A method to parse a property value from a string into the property List.
        /// Used when inserting new particular node.
        /// </summary>
        /// <param name="strProp"> A string value that will be parsed into the correct format and inserted into the List.</param>
        public abstract void ParsePropFromStringToList(string strProp);

        /// <summary>
        /// Clears contents of a property array.
        /// </summary>
        public abstract void ClearProperty();


        /// <summary>
        /// Returns string representation of a value stored on given index. 
        /// </summary>
        /// <param name="index"> An index of a value.</param>
        /// <returns> String representation of a value on given index. </returns>
        public abstract string GetValueAsString(int index);

        /// <summary>
        /// Returns a type of property.
        /// </summary>
        public abstract Type GetPropertyType();
    }

   

    
}
