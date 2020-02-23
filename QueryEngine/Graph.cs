/**
 * File includes definition of graph and its elements
 * Graph contains three lists... inward edges, outgoing edges and vertices.
 * Base class for nodes and edges is Element class, each element in a graph has got an ID and
 * a table (type).
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


    }


    /// <summary>
    /// Edge represents edge in a graph. The type of an edge is based on the list that contains the list.
    /// Each edge has an end vertex, that is which vertex the edge is leading to.
    /// </summary>
    class Edge : Element
    {
        public Vertex endVertex;
        public Vertex startVertex;

        public Edge()
        {
            this.id = -1;
            this.table = null;
            this.endVertex = null;
            this.startVertex = null;
        }

        public void SetPositionInEdges(int p) => this.positionInList = p;
        public void AddEndVertex(Vertex vertex) => this.endVertex = vertex;
        public Vertex GetEndVertex() => this.endVertex;
        public int GetPositionInEdges() => this.positionInList;
    }

    /// <summary>
    /// The class serves only for holder purpose during creation inside Processor.
    /// It enables us to pass a all the the required graph lists from withing one function.
    /// </summary>
    class EdgeListHolder
    {
        public List<Edge> outEdges;
        public List<Edge> inEdges;

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
        public List<Edge> outEdges;
        public List<Edge> inEdges;

        public Graph()
        {
            this.NodeTables = null;
            this.vertices = null;
            this.outEdges = null;
            this.inEdges = null;
            this.EdgeTables = null;
        }

     
        /// <summary>
        /// Loads all tables (types) from a file.
        /// </summary>
        private Dictionary<string, Table> LoadTables(string filename)
        {
            var reader = new Reader(filename);
            var processor = new TableDictProcessor();
            var creator = new CreatorFromFile<Dictionary<string, Table>>(reader, processor);
            return creator.Create();
        }
        public void LoadEdgeTables(string filename) => this.EdgeTables = LoadTables(filename);
        public void LoadNodeTables(string filename) => this.NodeTables = LoadTables(filename);

        public void LoadVertices(string filename)
        {
            var reader = new WordReader(filename);
            var processor = new VerticesListProcessor();
            processor.PassParameters(NodeTables);
            var creator = new CreatorFromFile<List<Vertex>>(reader, processor);
            this.vertices = creator.Create();
        }

        public void LoadEdges(string filename)
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
        public List<Edge> GetAllOutEdges() => this.outEdges;
        public List<Edge> GetAllInEdges() => this.inEdges;

        //TODO put somewhere else
        public void GetRangeToLastEdgeOfVertex(bool isOut, int positionOfVertex, out int start, out int end)
        {
            Vertex vertex = vertices[positionOfVertex];
            if (isOut)
            {
                start = vertex.GetOutEdgesStartPosition();
                end = vertex.GetOutEdgesEndPosition();
            }
            else
            {
                start = vertex.GetInEdgesStartPosition();
                end = vertex.GetInEdgesEndPosition();
            }
        }
    }
}
