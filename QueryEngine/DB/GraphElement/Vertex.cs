/*! \file 
Includes a definition of a vertex. 
The vertex is an element of a graph. Each vertex is stored in a vertex List in the graph.
Vertices are connected with edges. Each vertex knows the edges that are going outward and also inward.
The edges are contained in Lists (one List for outward and one List for inward edges) of edges.
The vertices contain information about the starting and the ending positions of edges in the structures containing the edges, so that
the edges can be accessed quickly.
 */

using System;

namespace QueryEngine
{
    /// <summary>
    /// A class Vertex serves as a vertex in a graph.
    /// Each vertex is stored in a vertex List in the graph.
    /// To each vertex there are two corresponding Lists of edges, one List includes outgoing edges,
    /// and the other encompasses inwards edges.
    /// If the vertex does not have any edges (in or out), then the corresponding positions (start and end) are set to -1.
    /// </summary>
    public sealed class Vertex : Element
    {
        public int OutEdgesStartPosition { get; internal set; }
        public int OutEdgesEndPosition { get; internal set; }
        public int InEdgesStartPosition { get; internal set; }
        public int InEdgesEndPosition { get; internal set; }
        
        /// <summary>
        /// Constructs a vertex with no edges.
        /// </summary>
        public Vertex(int id, Table table)
        {
            if (id <= 0)
                throw new ArgumentException($"{this.GetType()}, passed wrong id to constructor.");
            if (table == null)
                throw new ArgumentException($"{this.GetType()}, passed null as a table to constructor.");

            this.ID = id;
            this.Table = table;
            this.OutEdgesStartPosition = -1;
            this.OutEdgesEndPosition = -1;
            this.InEdgesStartPosition = -1;
            this.InEdgesEndPosition = -1;
            this.PositionInList = -1;
        }

        /// <summary>
        /// Constructs an empty vertex.
        /// </summary>
        public Vertex()
        {
            this.ID = -1;
            this.Table = null;
            this.OutEdgesStartPosition = -1;
            this.OutEdgesEndPosition = -1;
            this.InEdgesStartPosition = -1;
            this.InEdgesEndPosition = -1;
            this.PositionInList = -1; ;

        }

        public bool HasOutEdges() { if (this.OutEdgesStartPosition == -1) return false; else return true; }
        public bool HasInEdges() { if (this.InEdgesStartPosition == -1) return false; else return true; }

        /// <summary>
        /// Gets a range of out edges of this vertex.
        /// </summary>
        /// <param name="start"> The starting position of its edges in a edge List.</param>
        /// <param name="end"> The ending position of its edges in the same edge List.</param>
        public void GetRangeOfOutEdges(out int start, out int end)
        {
            start = this.OutEdgesStartPosition;
            end = this.OutEdgesEndPosition;
        }
        /// <summary>
        /// Gets a range of in edges of this vertex.
        /// </summary>
        /// <param name="start"> The starting position of its edges in a edge List.</param>
        /// <param name="end"> The ending position of its edges in the same edge List.</param>
        public void GetRangeOfInEdges(out int start, out int end)
        {
            start = this.InEdgesStartPosition;
            end = this.InEdgesEndPosition;
        }
    }
}
