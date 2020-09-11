/*! \file
  
File includes definition of tables.
Each graph element has a pointer to a type, that is to say, a table.
Each table holds all the nodes of the same type.
Table has got a list and two dictionaries.
The list for property names = property names in the same order they were added.
The dictionary of IDs, each entry is a representation of a graph element (element ID), on that entry lies the position of the element in table.
On the same position we will find values of properties of the element in the property lists.
The dictionary of properties is used for fast access to property via string.
  
Properties are formed from an abstract type Property that is visible from within a table.
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
     /// A class representing a single node type.
     /// Encompasses a table of properties pertaining to the type. 
     /// List IDs consists of added nodes into the table. Values of the elements can be found 
     /// on the same positions in the property lists.
     /// </summary>
    internal sealed class Table
    {
        /// <summary>
        /// Name of the table (type of node)
        /// </summary> 
        public string IRI { get; private set; } 

        /// <summary>
        /// Properties pertaining to the table.
        /// </summary>
        public Dictionary<string, Property> Properties { get; private set; }
       
        /// <summary>
        /// List of all property names in the table.
        /// </summary>
        public List<String> PropertyLabels {  get; private set; }

        /// <summary>
        /// Represents nodes inside a table. An index represents also an index inside the property lists.
        /// First int is an id of a node inside the table, and second int is the position inside the table.
        /// </summary>
        public Dictionary<int,int> IDs { get; private set; }

        /// <summary>
        /// Inits a new instance of a table.
        /// </summary>
        /// <param name="tableName"> A name that will be used as an identifier of the new table. </param>
        public Table(string tableName)
        {
            if (tableName == null) 
                throw new ArgumentException($"{this.GetType()}, table name not inicalised.");
            else
            { 
                this.IRI = tableName;
                this.Properties = new Dictionary<string, Property>();
                this.PropertyLabels = new List<string>();
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
            return this.PropertyLabels.Contains(iri); 
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
                this.PropertyLabels.Add(newProp.IRI);
                this.Properties.Add(newProp.IRI, newProp);
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
            if (!this.IDs.TryGetValue(elementId, out int elementPosition))
                throw new ArgumentException($"{this.GetType()}, element id = {elementId} not found in table.");

            if (this.Properties.TryGetValue(propertyName, out Property property)) 
                return property.GetValueAsString(elementPosition);
            else return "null";
        }

        /// <summary>
        /// Tries to get a value of a property based on index and the property name.
        /// </summary>
        /// <typeparam name="T"> Type of the accessed property. </typeparam>
        /// <param name="id"> Id of an element in the table. </param>
        /// <param name="propName"> Property name. </param>
        /// <param name="value"> Where to store the value of the property. </param>
        /// <returns>True if successful access, otherwise false. </returns>
        public bool TryGetPropertyValue<T>(int id, string propName, out T value)
        {
            if (!this.IDs.TryGetValue(id, out int elementPosition)) 
                throw new ArgumentException($"{this.GetType()}, accessing element that is missing in the table. Element ID = {id}.");
            else if (!this.Properties.TryGetValue(propName, out Property property))
            {
                value = default;
                return false;
            }
            else
            {
                value = ((Property<T>)property).propHolder[elementPosition];
                return true;
            }
        }
    }


   


}



