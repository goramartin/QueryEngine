/*! \file
  
  File includes definition of a specialised properties (e.i string, integer).
  Each speicalised property can parse it self from a string.

  There is a templated class property that inherits from a base property class.
  Subsequently, specialisations are created because of the need to treat different types
  differently.

  Each property contains a list of typed values.
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Represents typed property of a table.
    /// We create specialisations based on the type of T.
    /// Its functions are not visible from a table.
    /// </summary>
    abstract class Property<T> : Property
    {
        public List<T> propHolder;

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

        public override void ClearProperty()
        {
            this.propHolder.Clear();
        }

        /// <summary>
        /// Gets value on given index as a string.
        /// </summary>
        /// <param name="index"> Index of a row. </param>
        public override string GetValueAsString(int index)
        {
            return this.propHolder[index].ToString();
        }

        /// <summary>
        /// Returns type of this property.
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
    sealed class StringProperty : Property<string>
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
    sealed class IntProperty : Property<int>
    {
        public IntProperty(string propName) : base(propName) { }

        /// <summary>
        /// Tries to parse the number from a given string and stores it into a column.
        /// </summary>
        /// <param name="strProp">Value to store. </param>
        public override void ParsePropFromStringToList(string strProp)
        {
            int value = 0;
            if (!int.TryParse(strProp, out value))
                throw new ArgumentException($"{this.GetType()} Adding incorrect string to the int property.");

            this.propHolder.Add(value);
        }
    }
    #endregion PropertySpecialisations
}
