using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{
    /// <summary>
    /// Class representing single node type.
    /// Encompasses table of properties pertaining to the type. 
    /// List IDs consists of added nodes into the table. Values of the node we can find on the same position in the properties.
    /// </summary>
    class Table
    {
        private string tableIri;
        public string IRI 
        {
            get => this.tableIri; 
            protected set => this.tableIri=value; 
        }

        public List<Property> properties;
        public List<int> IDs;

        public Table(string tableName)
        {
            if (tableName == null) throw new ArgumentException($"{this.GetType()} Table name not inicalised.");
            else
            { 
                this.IRI = tableName;
                this.properties = new List<Property>();
                this.IDs = new List<int>();
            }
        }

        public int GetPropertyCount() { return this.properties.Count;  }

        public void AddID(int id) { this.IDs.Add(id); }

        public bool ContainsProperty(string iri)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].IRI == iri) return true;
            }
            return false;
        }
        public void AddNewProperty(Property newProp)
        {
            if (properties == null || newProp == null) 
                throw new ArgumentException($"{this.GetType()} Failed to add Property, list or prop not inicialised.");
           
            foreach (var item in properties)
            {
                if (newProp.IRI == item.IRI) 
                    throw new ArgumentException($"{this.GetType()} Adding property that already exists.");
            }
            this.properties.Add(newProp);
        }
    }

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
        /// <param name="strProp">Value that will be parsed into the correct formar and inserted into the list.</param>
        public abstract void ParsePropFromStringToList(string strProp);

        public abstract void ClearProperty();
    }


    abstract class Property<T>: Property
    {
        public List<T> propHolder; 
        
        public Property(string propName) 
        {
            if (propName == null) 
                throw new ArgumentException($"{this.GetType()} Property<T> name not inicalised.");
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
            if (registry.ContainsKey(token))
                throw new ArgumentException("PropertyFactory: Property Type already registered.");

            registry.Add(token, type);
        }

        public static Property CreateProperty(string token, string name)
        {
            if (!registry.ContainsKey(token)) 
                throw new ArgumentException("PropertyFactory: Token not found.");

            Type propType = null;
            if (registry.TryGetValue(token, out propType))
            {
                return (Property)Activator.CreateInstance(propType, name);
            }
            else throw new ArgumentException("PropertyFactory: Failed to load type from registry.");

        }
    }


}



