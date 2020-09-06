/*! \file 
Includes definition of a vertex. The vertex is an element of a graph. Each vertex is stored in a vertex list in the graph.
Vertices are connected with edges. Each vertex knows the edges that are going outward and also inward.
The edges are contained in lists (one list for outward and one list for inward edges) of edges in the database.
The vertices contain information about the starting and ending positions of edges in the structures containing the edges, so that
the edges can be accessed quickly.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Vertex serves as a node in a graph.
    /// Each vertex is stored in a vertex list in a graph.
    /// To each vertex there are two corresponding lists of edges, one list includes outgoing edges,
    /// and the other encompasses inwards edges.
    /// If the vertex does now have any edges (in or out), then the corresponding positions (start and end) are set to -1.
    /// </summary>
    internal sealed class Vertex : Element
    {
        public int OutEdgesStartPosition { get; internal set; }
        public int OutEdgesEndPosition { get; internal set; }
        public int InEdgesStartPosition { get; internal set; }
        public int InEdgesEndPosition { get; internal set; }
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
        /// Gets range of out edges of this vertex.
        /// </summary>
        /// <param name="start"> Starting position of its edges in a edge list.</param>
        /// <param name="end"> Ending position of its edges in the same edge list.</param>
        public void GetRangeOfOutEdges(out int start, out int end)
        {
            start = this.OutEdgesStartPosition;
            end = this.OutEdgesEndPosition;
        }
        /// <summary>
        /// Gets range of in edges of this vertex.
        /// </summary>
        /// <param name="start"> Starting position of its edges in a edge list.</param>
        /// <param name="end"> Ending position of its edges in the same edge list.</param>
        public void GetRangeOfInEdges(out int start, out int end)
        {
            start = this.InEdgesStartPosition;
            end = this.InEdgesEndPosition;
        }

        public override Type GetElementType()
        {
            return typeof(Vertex);
        }
    }
}
