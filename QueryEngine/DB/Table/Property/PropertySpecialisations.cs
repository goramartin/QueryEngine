/*! \file
This file includes definitions of specialised properties (e.i T = string, integer).
Each speicalised property can parse its values itself from a string.
Each property contains a List of typed values.
 */


using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Represents a typed property of a Table class.
    /// Its functions are not visible from a table.
    /// </summary>
    public abstract class Property<T> : Property
    {
        /// <summary>
        /// Contains values of the elements in the table.
        /// </summary>
        public List<T> propHolder;

        /// <summary>
        /// Constructs a property.
        /// </summary>
        /// <param name="propName"> An identifier of the property. </param>
        public Property(string propName)
        {
            if (propName == null)
                throw new ArgumentException($"{this.GetType()}, property name not inicalised.");
            else
            {
                this.propHolder = new List<T>();
                this.IRI = propName;
            }
        }

        /// <summary>
        /// Clears all values of all the elements in the table.
        /// </summary>
        public override void ClearProperty()
        {
            this.propHolder.Clear();
        }

        /// <summary>
        /// Gets a value on a given index as a string.
        /// </summary>
        /// <param name="index"> An index of a row. </param>
        public override string GetValueAsString(int index)
        {
            return this.propHolder[index].ToString();
        }

        /// <summary>
        /// Returns a type of this property.
        /// </summary>
        public override Type GetPropertyType()
        {
            return typeof(T);
        }

    }

    #region PropertySpecialisations

    /// <summary>
    /// String property specialisation.
    /// </summary>
    internal sealed class StringProperty : Property<string>
    {
        public StringProperty(string propName) : base(propName) { }

        /// <summary>
        /// Stores given string into a column.
        /// </summary>
        /// <param name="strProp">Value to store. </param>
        public override void ParsePropFromStringToList(string strProp)
        {
            if (strProp == null)
                throw new ArgumentException($"{this.GetType()} Adding empty string to the list of proprties.");
            this.propHolder.Add(strProp);
        }
    }

    /// <summary>
    /// Integer specialisation of a column.
    /// </summary>
    internal sealed class IntProperty : Property<int>
    {
        public IntProperty(string propName) : base(propName) { }

        /// <summary>
        /// Tries to parse the number from a given string and stores it into a column.
        /// </summary>
        /// <param name="strProp">Value to store. </param>
        public override void ParsePropFromStringToList(string strProp)
        {
            if (!int.TryParse(strProp, out int value))
                throw new ArgumentException($"{this.GetType()} Adding incorrect string to the int property.");

            this.propHolder.Add(value);
        }
    }
    #endregion PropertySpecialisations
}
