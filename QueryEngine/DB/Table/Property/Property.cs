/*! \file
File includes definition of a base class for a property that is visible to the Table class.
Properties are enclosed in the Table class.
Each property contains the name of the property defined in the json input schema.

Specialisations are generics and contain a List of values of the properties.
The base non generic property must be used to create a list of properties in the table,
because the types of properties are unknown before runtime.
 */

using System;

namespace QueryEngine
{
    /// <summary>
    /// Abstract property, holds only ID of a property (name).
    /// Its functions are visible to a table.
    /// </summary>
    internal abstract class Property
    {
        public string IRI { get; protected set; }

        public Property() { this.IRI = null; }

        /// <summary>
        /// Method to insert property value from the string into the property list.
        /// Used when inserting new particular node.
        /// </summary>
        /// <param name="strProp">Value that will be parsed into the correct format and inserted into the list.</param>
        public abstract void ParsePropFromStringToList(string strProp);

        /// <summary>
        /// Clears contents of a property array.
        /// </summary>
        public abstract void ClearProperty();


        /// <summary>
        /// Returns string representation of a value stored on given index. 
        /// </summary>
        /// <param name="index"> Index of a value.</param>
        /// <returns> String representation of a value on given index. </returns>
        public abstract string GetValueAsString(int index);

        /// <summary>
        /// Returns type of property.
        /// </summary>
        public abstract Type GetPropertyType();
    }

   

    
}
