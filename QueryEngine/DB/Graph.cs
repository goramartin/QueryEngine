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
        public int ID { get; internal set; }
        public Table Table { get; internal set; }
        public int PositionInList { get; internal set; }

        public void AddID(int id) => this.ID = id;
        public void AddTable(Table table) => this.Table = table;



        public override int GetHashCode()
        {
            return this.ID;
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
            this.InEdgesEndPosition= -1;
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

        public void GetRangeOfOutEdges(out int start, out int end)
        {
            start = this.OutEdgesStartPosition;
            end = this.OutEdgesEndPosition;
        }

        public void GetRangeOfInEdges(out int start, out int end)
        {
            start = this.InEdgesStartPosition;
            end = this.InEdgesEndPosition;
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
            var tmpTables = creator.Create();
            
            if (tmpTables.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, tables of the graph are empty. Filename = {filename}" );

            return tmpTables;
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

            if (this.vertices.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, vertices of the graph are empty. Filename = {filename}");
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

            if (this.outEdges.Count == 0) 
                throw new ArgumentException($"{this.GetType()} Out edges of the graph are empty. Filename = {filename}");
            if (this.inEdges.Count == 0) 
                throw new ArgumentException($"{this.GetType()} In edges of the graph are empty. Filename = {filename}");

        }


        public List<Vertex> GetAllVertices() => this.vertices;
        public List<OutEdge> GetAllOutEdges() => this.outEdges;
        public List<InEdge> GetAllInEdges() => this.inEdges;

    }
}
