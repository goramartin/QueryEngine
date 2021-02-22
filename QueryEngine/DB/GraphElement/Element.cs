/*! \file 
Includes definition of a base class for each element in the graph.
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
    /// A base class every graph element.
    /// The table represents the type of the element.
    /// The element data are stored in the table as well.
    /// The position represents a position in a enclosing structure (In our case it is a List).
    /// </summary>
    public abstract class Element
    {
        public int ID { get; internal set; }
        public Table Table { get; internal set; }

        public int PositionInList { get; internal set; }

        public override int GetHashCode()
        {
            return this.ID;
        }
    }
}
