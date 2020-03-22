
/**
 * File includes definition of tables and property types.
 * Each node has a pointer to a type, that is to say, a table.
 * Each table holds all the nodes of the same type.
 * Table has two lists, one for properties - names lists with values of a single type of a node,
 * list of IDs, each index is a representation of a node, on that index lies the real ID.
 * On the same index we will find values of properties of the node in the property lists. 
 * 
 * Properties are form from an abstract type Property that is visible from within a table.
 * Generic properties extend Property, and specialisations are created separately. 
 * Properties are created with a help of an Activator class based on a passed name.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{
     /// <summary>
    // Class representing single node type.
    // Encompasses table of properties pertaining to the type. 
    // List IDs consists of added nodes into the table. Values of the node we can find 
    // on the same position in the properties.
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

        // Properties pertaining to a table.
        public List<Property> properties { get; private set;}
        
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
                this.properties = new List<Property>();
                this.IDs = new Dictionary<int, int>();
            }
        }

        public int GetPropertyCount() { return this.properties.Count;  }

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
        /// Checks whether a given property name is set on a table.
        /// </summary>
        /// <param name="iri">Property about to be searched for.</param>
        public bool ContainsProperty(string iri)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].IRI == iri) return true;
            }
            return false;
        }

        /// <summary>
        /// Adds new property into a table.
        /// Throws when the property is already inside.
        /// </summary>
        /// <param name="newProp">Property to be added into a table.</param>
        public void AddNewProperty(Property newProp)
        {
            if (properties == null || newProp == null) 
                throw new ArgumentException($"{this.GetType()}, failed to add Property, list or prop not inicialised.");
           
            foreach (var item in properties)
            {
                if (newProp.IRI == item.IRI) 
                    throw new ArgumentException($"{this.GetType()}, adding property that already exists. Prop name = {newProp.IRI}");
            }
            this.properties.Add(newProp);
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

        public abstract void ClearProperty();
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
    }

#region PropertySpecialisations

    class StringProperty : Property<string>
    {
        public StringProperty(string propName): base(propName) { }

        public override void ParsePropFromStringToList(string strProp)
        {
            if (strProp == null) 
               throw new ArgumentException($"{this.GetType()} Adding empty string to the list of proprties.");
            this.propHolder.Add(strProp);
        }
    }

    class IntProperty : Property<int>
    {
        public IntProperty(string propName): base(propName) { }

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
    /// Class includes register of all the property types.
    //  Enables to create instance of a property based on a string token.
    /// </summary>
    static class PropertyFactory
    {
        static Dictionary<string, Type> registry;
        
        static PropertyFactory()
        {
            registry = new Dictionary<string, Type>();
            InicialiseRegistry();
        }

        private static void InicialiseRegistry()
        {
            RegisterProperty("string", typeof(StringProperty));
            RegisterProperty("integer", typeof(IntProperty));

        }

         private static void RegisterProperty(string token, Type type)
        {
            if (token == null || type == null)
                throw new ArgumentException($"PropertyFactory, cannot register null type or null token.");

            if (registry.ContainsKey(token))
                throw new ArgumentException($"PropertyFactory, property Type already registered. Token = {token}");

            registry.Add(token, type);
        }

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



