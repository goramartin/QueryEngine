/*! \file 
Includes definition of a base class for each element in the graph.
Each element needs an unique ID in the entire graph, table (type of an element) where the properties
of the element are stored and a position in the containing structure (such as an index in a list).
Hash codes are based on the unique ID of the element.
Properties of elements are accessed through generic method which is given the name of the property 
and a type of the accessing property. The method then calls methods of the table to retrieve the 
data from the database.

Note that the ID of an element is not directly a property in the table.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A base class for edges and nodes.
    /// The table represents the type of the element.
    /// The element data are stored in the table as well.
    /// The position is a location inside a vertex list or a node list.
    /// </summary>
    internal abstract class Element
    {
        public int ID { get; internal set; }
        public Table Table { get; internal set; }

        /// <summary>
        /// Represents position in a enclosing structure.
        /// </summary>
        public int PositionInList { get; internal set; }

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

        /// <summary>
        /// This method returns type of a graph element.
        /// It is used instead of the reflection method which is a bit slower.
        /// </summary>
        /// <returns></returns>
        public abstract Type GetElementType();

    }
}
