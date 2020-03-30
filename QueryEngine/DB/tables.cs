/*! \file
  
  File includes definition of tables and property types.
  Each node has a pointer to a type, that is to say, a table.
  Each table holds all the nodes of the same type.
  Table has got one list,dictionary and a hash table, list for properties = named lists with values of a single type of a node,
  dictionary of IDs, each object is a representation of a node, on that object lies the position index of the element in table.
  On the same index we will find values of properties of the node in the property lists.
  And a hash set for fast access to property labels.
  
  Properties are form from an abstract type Property that is visible from within a table.
  Generic properties extend Property, and specialisations are created separately. 
  Properties are created with a help of an Activator class based on a passed name.
  
  This file contains also static factory for creation of tables.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{
     /// <summary>
     /// Class representing single node type.
     /// Encompasses table of properties pertaining to the type. 
     /// List IDs consists of added nodes into the table. Values of the node we can find 
     /// on the same position in the properties.
     /// </summary>
    class Table
    {
        // Name of the table (type of node)
        private string tableIri;
        public string IRI 
        {
            get => this.tableIri; 
            protected set => this.tableIri=value; 
        }

        /// <summary>
        /// Properties pertaining to the table.
        /// </summary>
        public List<Property> Properties { get; private set; }

        /// <summary>
        /// Contains labels of properties for faster search and access.
        /// string for a name of a property and stored integer represents index in a list properties.
        /// </summary>
        private Dictionary<string, int> PropertyLabels {  get; set; }

        // Represents nodes inside a table. An index represents also an index inside the property lists.
        // First int is an id of a node inside the table, and second int is the position inside the table.
        public Dictionary<int,int> IDs { get; private set; }

        public Table(string tableName)
        {
            if (tableName == null) 
                throw new ArgumentException($"{this.GetType()}, table name not inicalised.");
            else
            { 
                this.IRI = tableName;
                this.Properties = new List<Property>();
                this.PropertyLabels = new Dictionary<string, int>();
                this.IDs = new Dictionary<int, int>();
            }
        }

        public int GetPropertyCount() { return this.Properties.Count;  }

        /// <summary>
        /// Adds id of a node into the table. Each id is bound with the position inside the table.
        /// Tuple int, int is ment for: First int is the id of the node and the second int is the position inside this table.
        /// </summary>
        /// <param name="id"> Unique id of a node. </param>
        public void AddID(int id) 
        {
            if (this.IDs.ContainsKey(id))
                throw new ArgumentException($"{this.GetType()}, table {this.IRI} already contains id of a node {id}.");
            else this.IDs.Add(id, this.IDs.Count);
        }

        /// <summary>
        /// Gets position of an element in the table based its id.
        /// </summary>
        /// <param name="element"> Graph element.</param>
        /// <returns> Position of a given element in the table. </returns>
        public int GetElementPosition(Element element)
        {
            if (!this.IDs.TryGetValue(element.ID, out int value))
                throw new ArgumentException($"{this.GetType()}, table {this.IRI} does not contains id of a node {element.ID}.");
            return value;
        }

        /// <summary>
        /// Checks whether a given property name is set on a table.
        /// </summary>
        /// <param name="iri">Property about to be searched for.</param>
        /// <returns> True if found. </returns>
        public bool ContainsProperty(string iri)
        {
            return this.PropertyLabels.ContainsKey(iri);
        }


        /// <summary>
        /// Adds new property into a table.
        /// Throws when the property is already inside.
        /// </summary>
        /// <param name="newProp">Property to be added into a table.</param>
        public void AddNewProperty(Property newProp)
        {
            if (Properties == null || newProp == null)
                throw new ArgumentException($"{this.GetType()}, failed to add Property, list or prop not inicialised.");
            else if (this.ContainsProperty(newProp.IRI))
                throw new ArgumentException($"{this.GetType()}, adding property that already exists. Prop name = {newProp.IRI}");
            else
            {
                this.PropertyLabels.Add(newProp.IRI, this.Properties.Count);
                this.Properties.Add(newProp);
            }
        }


        /// <summary>
        /// Based on id of an element and property name, it reaches to the property array
        /// and returns stored value as a string. 
        /// </summary>
        /// <param name="elementId"> Id of an element inside a table. </param>
        /// <param name="propertyName"> Name of accessed property. </param>
        /// <returns> String value of value stored inside a property or null if property does not exists.</returns>
        public string TryGetElementValueAsString(int elementId, string propertyName)
        {
            if (!this.IDs.TryGetValue(elementId, out int value))
                throw new ArgumentException($"{this.GetType()}, element id = {elementId} not found in table.");

            if (this.PropertyLabels.TryGetValue(propertyName, out int propIndex)) 
                return this.Properties[propIndex].GetValueAsString(value);
            else return "null";
        }


    }


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
    }

    /// <summary>
    /// Represents typed property of a table.
    /// We create specialisations based on the type of T.
    /// Its functions are not visible from a table.
    /// </summary>
    abstract class Property<T>: Property
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
    }

#region PropertySpecialisations

    /// <summary>
    /// String property specialisation.
    /// </summary>
    class StringProperty : Property<string>
    {
        public StringProperty(string propName): base(propName) { }

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
    class IntProperty : Property<int>
    {
        public IntProperty(string propName): base(propName) { }

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

    /// <summary>
    /// Property factory.
    /// Class includes register of all the property types.
    /// Enables to create instance of a property based on a string token.
    /// </summary>
    static class PropertyFactory
    {
        /// <summary>
        /// Register with valid value types.
        /// </summary>
        static Dictionary<string, Type> registry;
        
        /// <summary>
        /// Inicialises registry.
        /// </summary>
        static PropertyFactory()
        {
            registry = new Dictionary<string, Type>();
            InicialiseRegistry();
        }

        /// <summary>
        /// Inicialises registry with predefined values.
        /// </summary>
        private static void InicialiseRegistry()
        {
            RegisterProperty("string", typeof(StringProperty));
            RegisterProperty("integer", typeof(IntProperty));

        }

        /// <summary>
        /// Registers a property with a given token and bounds a given type to the string.
        /// </summary>
        /// <param name="token"> Property type. </param>
        /// <param name="type"> Type of property type. </param>
         private static void RegisterProperty(string token, Type type)
        {
            if (token == null || type == null)
                throw new ArgumentException($"PropertyFactory, cannot register null type or null token.");

            if (registry.ContainsKey(token))
                throw new ArgumentException($"PropertyFactory, property Type already registered. Token = {token}");

            registry.Add(token, type);
        }

        /// <summary>
        /// Creates an instance of a property based on a given token.
        /// </summary>
        /// <param name="token"> Property type </param>
        /// <param name="name"> Type of a property type. </param>
        /// <returns></returns>
        public static Property CreateProperty(string token, string name)
        {
            if (token == null || name == null)
                throw new ArgumentException($"PropertyFactory, passed null name or null token.");

            if (!registry.ContainsKey(token)) 
                throw new ArgumentException($"PropertyFactory, token not found. Token = {token}.");

            Type propType = null;
            if (registry.TryGetValue(token, out propType))
            {
                return (Property)Activator.CreateInstance(propType, name);
            }
            else throw new ArgumentException($"PropertyFactory, failed to load type from registry. Type = {name}.");

        }
    }


}



