/**
 * File includes definition of graph and its elements
 * Graph contains three lists... inward edges, outgoing edges and vertices.
 * Base class for nodes and edges is Element class, each element in a graph has got an ID and
 * a table (type). Also each element knows its position in the list where it is included. 
 * 
 *  Each vertex has got a positions for edges in edge lists, one positions for incoming edges and 
 *  one for outgoing edges. Starting position means that on that position the edge from this vertex is leading and
 *  end position means that on that position, edges from a consecutive vertex are starting.
 * 
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
        public int id;
        public Table table;
        public int positionInList;

        public void AddID(int id) => this.id = id;
        public void AddTable(Table table) => this.table = table;

        public Table GetTable() => this.table;
        public int GetID() => this.id;

        public override int GetHashCode()
        {
            return this.id;
        }

    }

    /// <summary>
    /// Vertex serves as a node in a graph.
    /// Each vertex is stored in a vertex list in a graph.
    /// TO each vertex there are two corresponding lists of edges, one list includes outgoing edges,
    /// and the other one encompasses inwards edges.
    /// If the vertex does now have any edges (in or out) the positions are set to -1.
    /// </summary>
    class Vertex : Element
    {
        public int outEdgesStartPosition;
        public int outEdgesEndPosition;
        public int inEdgesStartPosition;
        public int inEdgesEndPosition;
        public Vertex(int id, Table table)
        {
            this.id = id;
            this.table = table;
            this.outEdgesStartPosition = -1;
            this.outEdgesEndPosition = -1;
            this.inEdgesStartPosition = -1;
            this.inEdgesEndPosition= -1;
            this.positionInList = -1;
        }

        public Vertex()
        {
            this.id = -1;
            this.table = null;
            this.outEdgesStartPosition = -1;
            this.outEdgesEndPosition = -1;
            this.inEdgesStartPosition = -1;
            this.inEdgesEndPosition = -1;
            this.positionInList = -1; ;

        }

        public void SetPositionInVertices(int position) => this.positionInList = position;
        public void SetOutEdgesStartPosition(int position) => this.outEdgesStartPosition = position;
        public void SetOutEdgesEndPosition(int count) => this.outEdgesEndPosition = count;
        public void SetInEdgesStartPosition(int position) => this.inEdgesStartPosition = position;
        public void SetInEdgesEndPosition(int count) => this.inEdgesEndPosition = count;

        public bool HasOutEdges() { if (this.outEdgesStartPosition == -1) return false; else return true; }
        public bool HasInEdges() { if (this.inEdgesStartPosition == -1) return false; else return true; }
        
        public int GetPositionInVertices() => this.positionInList;
        public int GetOutEdgesStartPosition() => this.outEdgesStartPosition;
        public int GetOutEdgesEndPosition() => this.outEdgesEndPosition;
        public int GetInEdgesStartPosition() => this.inEdgesStartPosition;
        public int GetInEdgesEndPosition() => this.inEdgesEndPosition;

        public void GetRangeOfOutEdges(out int start, out int end)
        {
            start = this.GetOutEdgesStartPosition();
            end = this.GetOutEdgesEndPosition();
        }

        public void GetRangeOfInEdges(out int start, out int end)
        {
            start = this.GetInEdgesStartPosition();
            end = this.GetInEdgesEndPosition();
        }


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }


    /// <summary>
    /// Edge represents edge in a graph. The type of an edge is based on the list that contains the list.
    /// Each edge has an end vertex, that is which vertex the edge is leading to.
    /// </summary>
    enum EdgeType { NotEdge, InEdge, OutEdge, AnyEdge };
    class Edge : Element
    {
        public EdgeType edgeType;
        public Vertex endVertex;

        public Edge()
        {
            this.id = -1;
            this.table = null;
            this.endVertex = null;
        }

        public void SetPositionInEdges(int p) => this.positionInList = p;
        public void AddEndVertex(Vertex vertex) => this.endVertex = vertex;
        public Vertex GetEndVertex() => this.endVertex;
        public int GetPositionInEdges() => this.positionInList;

        public EdgeType GetEdgeType() => this.edgeType;
        public void SetEdgeType(EdgeType type) => this.edgeType = type;


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


    }

    class InEdge : Edge
    {
        public InEdge() : base()
        {
            this.edgeType = EdgeType.InEdge;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

    class OutEdge : Edge
    {
        public OutEdge() : base()
        {
            this.edgeType = EdgeType.OutEdge;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }



    /// <summary>
    /// The class serves only for holder purpose during creation inside Processor.
    /// It enables us to pass a all the the required graph lists from withing one function.
    /// </summary>
    class EdgeListHolder
    {
        public List<OutEdge> outEdges;
        public List<InEdge> inEdges;

        public EdgeListHolder() {this.outEdges = null; this.inEdges = null; }
    }


    /// <summary>
    /// Graph contains vertices, outgoing edges and inwards edges, also types of elements inside a graph.
    /// Also gives interface to a whole graph.
    /// </summary>
    class Graph
    {
        public Dictionary<string, Table> NodeTables;
        public Dictionary<string, Table> EdgeTables;
        public List<Vertex> vertices;
        public List<OutEdge> outEdges;
        public List<InEdge> inEdges;

        public Graph(string[] args)
        {
            this.NodeTables = null;
            this.vertices = null;
            this.outEdges = null;
            this.inEdges = null;
            this.EdgeTables = null;

            this.LoadNodeTables("NodeTypes.txt");
            this.LoadEdgeTables("EdgeTypes.txt");
            this.LoadVertices("Nodes.txt");
            this.LoadEdges("Edges.txt");
        }



        /// <summary>
        /// Loads table types from a file. 
        /// </summary>
        /// <param name="filename"> A file containing definitions of tables. </param>
        /// <returns> Dictionary of tables. </returns>
        private Dictionary<string, Table> LoadTables(string filename)
        {
            var reader = new Reader(filename);
            var processor = new TableDictProcessor();
            var creator = new CreatorFromFile<Dictionary<string, Table>>(reader, processor);
            return creator.Create();
        }
        private void LoadEdgeTables(string filename) => this.EdgeTables = LoadTables(filename);
        private void LoadNodeTables(string filename) => this.NodeTables = LoadTables(filename);

        /// <summary>
        /// Loads all vetices from a file.
        /// </summary>
        /// <param name="filename"> File containing data of vertices. </param>
        private void LoadVertices(string filename)
        {
            var reader = new WordReader(filename);
            var processor = new VerticesListProcessor();
            processor.PassParameters(NodeTables);
            var creator = new CreatorFromFile<List<Vertex>>(reader, processor);
            this.vertices = creator.Create();
        }


        /// <summary>
        /// Loads all vetices from a file.
        /// </summary>
        /// <param name="filename"> File containing data of edges. </param>
        private void LoadEdges(string filename)
        {
            var reader = new WordReader(filename);
            var processor = new EdgeListProcessor();
            processor.PassParameters(NodeTables, EdgeTables, vertices);
            var creator = new CreatorFromFile<EdgeListHolder>(reader, processor);
            var result = creator.Create();
            this.outEdges = result.outEdges;
            this.inEdges = result.inEdges;
        }


        public List<Vertex> GetAllVertices() => this.vertices;
        public List<OutEdge> GetAllOutEdges() => this.outEdges;
        public List<InEdge> GetAllInEdges() => this.inEdges;

    }
}
