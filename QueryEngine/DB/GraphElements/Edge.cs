/*! \file 
  Includes definition of a edge in the graph.
  Edge is an element of a graph.
  All edges are oriented edges in the graph.
  So far there are only out edges and inward edges.

  Out edges are defined in the input file.
  In edges are former after out edges are created. (There are only the starting positions of the edge)
  Note that if there is an edge  1 -> 2 with ID 7, then out edge end vertex is 2 and in edge vertex is 1,
  however the id of in and out edge is the same.

  Each edge knows the ending vertex.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /*! \enum EdgeType
	
	Represents all possible types of edge in a graph.
        Not an edge is a value that is used to create match object with before they are assigned a proper edge type.
    */
    enum EdgeType { NotEdge, InEdge, OutEdge, AnyEdge };

    /// <summary>
    /// Edge represents edge in a graph. The type of an edge is based on the list that contains the list.
    /// Each edge has an end vertex, that is which vertex the edge is leading to.
    /// </summary>
    class Edge : Element
    {
        public EdgeType EdgeType { get; internal set; }
        public Vertex EndVertex { get; internal set; }

        public Edge()
        {
            this.ID = -1;
            this.Table = null;
            this.EndVertex = null;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// In specialisation of an edge.
    /// </summary>
    class InEdge : Edge
    {
        public InEdge() : base()
        {
            this.EdgeType = EdgeType.InEdge;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

    /// <summary>
    /// Out specialisation of an edge.
    /// </summary>
    class OutEdge : Edge
    {
        public OutEdge() : base()
        {
            this.EdgeType = EdgeType.OutEdge;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
