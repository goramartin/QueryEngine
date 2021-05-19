/*! \file 
Includes a definition of an edge in a graph. 
The edge is an element of the graph.

All edges are oriented edges. 

So far there are only out edges and in edges (only the semantic meaning is different).
Edges are defined in a file.
The file contains only out edges.
The inward edges are constructed based on the out edges.
Both types are contained in a list in the class Graph.

The inward edges are created and assigned to appropriate vertices by using information from the newly created out edges.
Each edge knows the ending vertex it points to. So each vertex has assigned its out an in edges.
For example, if there is an edge  1 -> 2 with ID 7, then out edge EndVertex is 2 and in edge EndVertex is 1
However, the IDs of an in edge and an out edge is the same because they point to the same properties in the database.
 */

namespace QueryEngine
{

    /// <summary>
    /// A class Edge represents an edge in a graph.
    /// Each edge has an end vertex, that is a vertex the edge is pointing to.
    /// The specialisations are used namely to differentiate the semantic meaning, because
    /// during searching of a graph, there is a difference in semantics whether an out or an in edge is picked.
    /// </summary>
    public abstract class Edge : Element
    {
        /// <summary>
        /// A vertex the edge points to.
        /// </summary>
        public Vertex EndVertex { get; internal set; }

        /// <summary>
        /// Constructs an edge with default values.
        /// </summary>
        public Edge()
        {
            this.ID = -1;
            this.Table = null;
            this.EndVertex = null;
        }
    }

    /// <summary>
    /// In specialisation of an edge.
    /// </summary>
    public sealed class InEdge : Edge
    {
        /// <summary>
        /// Constructs an empty edge.
        /// </summary>
        public InEdge() : base()
        { }
    }

    /// <summary>
    /// Out specialisation of an edge.
    /// </summary>
    public sealed class OutEdge : Edge
    {
        /// <summary>
        /// Constructs an empty edge.
        /// </summary>
        public OutEdge() : base() 
        { }
    }
}
