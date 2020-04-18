/*! \file
  
  File includes definition of a base class for a property.
  Each property contains the name of the property defined in the json input scheme.
  It also form an interface to the table that enclose the property.
  

 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Abstract property, holds only id of a property (name).
    /// Its functions are visible from a table.
    /// </summary>
    abstract class Property
    {
        private string propiri;
        public string IRI
        {
            get => this.propiri;
            protected set => this.propiri = value;
        }

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
