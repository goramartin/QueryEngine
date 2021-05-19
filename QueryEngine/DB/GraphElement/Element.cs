/*! \file 
Includes a definition of a base class for each element in the graph.
Each element needs an unique ID in the entire graph, table (type of an element) where the properties
of the element are stored and a position in the containing structure (such as an index in a list).

Hash codes are based on the unique ID of the element.

Properties of elements are accessed through generic method which is given the name of the property (property ID) 
and a type of the accessing property. The method then calls methods of the table to retrieve the 
data.

Note that the ID of an element is not directly a property in the table.

 */

namespace QueryEngine
{
    /// <summary>
    /// A base class of every graph element.
    /// The element data are stored in the Table.
    /// It assumes that the elements are stored in a List.
    /// </summary>
    public abstract class Element
    {
        /// <summary>
        /// A unique ID in the entire graph.
        /// </summary>
        public int ID { get; internal set; }
        /// <summary>
        /// A type of the element used to access it's properties.
        /// </summary>
        public Table Table { get; internal set; }

        /// <summary>
        /// The position represents a position in an enclosing structure (in our case it is List).
        /// This enables to access bordering elements faster.
        /// </summary>
        public int PositionInList { get; internal set; }

        /// <summary>
        /// Hash is based on the ID of the element.
        /// </summary>
        public override int GetHashCode()
        {
            return this.ID;
        }
    }
}
