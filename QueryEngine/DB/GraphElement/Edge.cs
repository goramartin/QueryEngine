/*! \file 
Includes definition of an edge in the graph. Edge is an element of the graph.

All edges are oriented edges in the graph. 

So far there are only out edges and inward edges (only the semantic meaning is different).
The outward edges, outgoing edges from vertices, are defined in a file. 
The List containg outward edges is created first and then
the inward edges are created and assigned to appropriate vertices by using information from the newly created out edges.
Each edge knows the ending vertex it points to. So each vertex has assigned its out an in edges.
For example, if there is an edge  1 -> 2 with ID 7, then out edge EndVertex is 2 and in edge EndVertex is 1
However, the IDs of an in edge and an out edge is the same because they point to the same properties in the database.
 */

namespace QueryEngine
{

    /// <summary>
    /// Edge represents edge in a graph.
    /// Each edge has an end vertex, that is a a vertex the edge is pointing to.
    /// The specialisations are used namely to differentiate the semantic meaning, because
    /// during searching of a graph, there is a difference whether an out or an in edge is picked.
    /// </summary>
    public abstract class Edge : Element
    {
        /// <summary>
        /// A vertex the edge points towards.
        /// </summary>
        public Vertex EndVertex { get; internal set; }

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
        public InEdge() : base()
        {
        }
    }

    /// <summary>
    /// Out specialisation of an edge.
    /// </summary>
    public sealed class OutEdge : Edge
    {
        public OutEdge() : base() { }
    }
}
