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
    }
}
