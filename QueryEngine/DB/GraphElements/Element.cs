/*! \file 
  Includes definition of a base class for each element in the graph.
  Each elements need an unique ID in the entire graph, table where the properties
  of the element are stored and a position in the containing structure.

  Hash codes are based on the unique id of the element.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for edges and nodes.
    /// Table represents the table where is the element stored.
    /// Position is location inside a vertex list or node list.
    /// NOTICE id of can be same for vertex and edge.
    /// </summary>
    abstract class Element
    {
        public int ID { get; internal set; }
        public Table Table { get; internal set; }

        /// <summary>
        /// Represents position in a enclosing structure.
        /// </summary>
        public int PositionInList { get; internal set; }

        public void AddID(int id) => this.ID = id;
        public void AddTable(Table table) => this.Table = table;

        public override int GetHashCode()
        {
            return this.ID;
        }

        /// <summary>
        /// Gets property value of an element based on property name.
        /// </summary>
        /// <typeparam name="T"> Type of property.</typeparam>
        /// <param name="propName"> Property name. </param>
        /// <param name="value"> Vale where to store the property value on success, </param>
        /// <returns> True if the property is on the table otherwise false. </returns>
        public bool TryGetPropertyValue<T>(string propName, out T value)
        {
            return this.Table.TryGetPropertyValue(this.ID, propName,out value);
        }
    }
}
