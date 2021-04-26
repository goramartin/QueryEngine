/*! \file
The file includes definition of tables.
Each graph element has a pointer to a type, that is to say, a table.
Each table holds all the nodes of the same type.
Table has got a List and two Dictionaries.
The List for property IDs = property IDs in the same order as they were added.
The Dictionary of IDs, each entry is a representation of a graph element (element ID), on that entry lies the position of the element in table.
On the same position we will find values of properties of the element in the property lists.
The Dictionary of properties is used for fast access to property via propID.
  
Properties are formed from an abstract type Property that is visible from within a table.
Generic properties extend Property, and specialisations are created separately. 
Properties are created with a help of an Activator class based on a passed name.
Properties are access via property ids that were assigned to them during schema parsing.
That means that when accessing properties, only their IDs are used, while graph class holds a
map of the ids associated with their string names. 
 */

using System;
using System.Collections.Generic;

namespace QueryEngine
{
     /// <summary>
     /// A class representing a single node type.
     /// Encompasses a table of properties pertaining to the type. 
     /// List IDs consists of added nodes into the table. Values of the elements can be found 
     /// on the same positions in the property lists.
     /// </summary>
    public sealed class Table
    {
        /// <summary>
        /// Name of the table (type of node)
        /// </summary> 
        public string IRI { get; private set; } 

        /// <summary>
        /// Properties pertaining to the table.
        /// </summary>
        public Dictionary<int, Property> Properties { get; private set; }
       
        /// <summary>
        /// List of all property IDs in the table.
        /// In the order of creation.
        /// </summary>
        public List<int> PropertyLabels {  get; private set; }

        /// <summary>
        /// Represents nodes inside a table. An index represents also an index inside the property lists.
        /// First int is an id of a node inside the table, and second int is the position inside the table.
        /// </summary>
        public Dictionary<int,int> IDs { get; private set; }

        /// <summary>
        /// Returns a number of properties of the table.
        /// </summary>
        public int PropertyCount => this.Properties.Count;  
       
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
                this.Properties = new Dictionary<int, Property>();
                this.PropertyLabels = new List<int>();
                this.IDs = new Dictionary<int, int>();
            }
        }


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
        /// <param name="propID"> The property about to be searched for.</param>
        /// <returns> True if found. </returns>
        public bool ContainsProperty(int propID)
        {
            return this.PropertyLabels.Contains(propID); 
        }


        /// <summary>
        /// Adds new property into a table.
        /// Throws when the property is already inside.
        /// </summary>
        /// <param name="propID"> An Id used to access property via a dictionary.</param>
        /// <param name="newProp"> A property to be added into a table.</param>
        public void AddNewProperty(int propID, Property newProp)
        {
            if (Properties == null || newProp == null)
                throw new ArgumentException($"{this.GetType()}, failed to add Property, list or prop not inicialised.");
            else if (this.ContainsProperty(propID))
                throw new ArgumentException($"{this.GetType()}, adding property that already exists. Prop name = {newProp.IRI}");
            else
            {
                this.PropertyLabels.Add(propID);
                this.Properties.Add(propID, newProp);
            }
        }

        /// <summary>
        /// Tries to get a value of a property based on index and the property name.
        /// Note that this method does not checks whether the id exists in the table.
        /// The omission is done because the table is accessed via elements.
        /// Thus, if the access was wrong, than the entire loaded graph is in bad format.
        /// </summary>
        /// <typeparam name="T"> Type of the accessed property. </typeparam>
        /// <param name="id"> Id of an element in the table. </param>
        /// <param name="propID"> Accessed property ID. </param>
        /// <param name="retValue"> Where to store the value of the property. </param>
        /// <returns>True if successful access, otherwise false. </returns>
        public bool TryGetPropertyValue<T>(int id, int propID, out T retValue)
        {
            if (!this.Properties.TryGetValue(propID, out Property property))
            {
                retValue = default;
                return false;
            }
            else
            {
                retValue = ((Property<T>)property).propHolder[this.IDs[id]];
                return true;
            }
        }
    }


   


}



