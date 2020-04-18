/*! \file
  
  File includes definition of tables.
  Each graph element has a pointer to a type, that is to say, a table.
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

        /// <summary>
        /// Tries to get a value of a property based on index and the property name.
        /// </summary>
        /// <typeparam name="T"> Type of the accessed property. </typeparam>
        /// <param name="id"> Id of an element in the table. </param>
        /// <param name="propName"> Property name. </param>
        /// <param name="value"> Where to store the value of the property. </param>
        /// <returns>True of successful access otherwise false. </returns>
        public bool TryGetPropertyValue<T>(int id, string propName, out T value)
        {
            if (!this.IDs.TryGetValue(id, out int elementPosition)) 
                throw new ArgumentException($"{this.GetType()}, accessing element that is missing in the table. Element ID = {id}.");
            else if (!this.PropertyLabels.TryGetValue(propName, out int propertyPosition))
            {
                value = default(T);
                return false;
            } else
            {
                value = ((Property<T>)(this.Properties[propertyPosition])).propHolder[elementPosition];
                return true;
            }
        }
    }


   


}



