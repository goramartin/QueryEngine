using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    //One field in edge list... that is one field in list of vertices and one filed in list of edges
     abstract class Element
    {
        public int id;
        public Table table;

        public void AddID(int id) => this.id = id;
        public void AddTable(Table table) => this.table = table;

        public Table GetTable() => this.table;
        public int GetID() => this.id;
    }


    class Vertex : Element
    {

        public int edgePosition;
        public List<Edge> incomingEdges;
        public Vertex(int id, Table table)
        {
            this.id = id;
            this.table = table;
            this.edgePosition = -1;
            this.incomingEdges = new List<Edge>();
        }

        public Vertex()
        {
            this.id = -1;
            this.table = null;
            this.edgePosition = -1;
            this.incomingEdges = new List<Edge>();

        }

        public void SetEdgePosition(int position) => this.edgePosition = position;
        public bool HasEdges() { if (this.edgePosition == -1) return false; else return true; }
        public void AddIncomingEdge(Edge e) { this.incomingEdges.Add(e); }

        public List<Edge> GetIncomingEdges() => this.incomingEdges;
        public int GetEdgePosition() => this.edgePosition;

    }

    class Edge : Element
    {
        public Vertex endVertex;

        public Edge(int id, Table table, Vertex vertex)
        {
            this.id = id;
            this.table = table;
            this.endVertex = vertex;
        }
        public Edge()
        {
            this.id = -1;
            this.table = null;
            this.endVertex = null;
        }

        public void AddEndVertex(Vertex vertex) => this.endVertex = vertex;
        public Vertex GetEndVertex() => this.endVertex;
    }

    //Only for holder purpose during creation inside Processor
    class EdgeListHolder
    {
        public List<Vertex> vertices;
        public List<Edge> edges;

        public EdgeListHolder() { this.vertices = null; this.edges = null; }
    }

    class Graph
    {
        public Dictionary<string, Table> NodeTables;
        public Dictionary<string, Table> EdgeTables;
        public List<Vertex> vertices;
        public List<Edge> edges;

        public Graph()
        {
            this.NodeTables = null;
            this.vertices = null;
            this.edges = null;
            this.EdgeTables = null;
        }

        private Dictionary<string, Table> LoadTables(string filename)
        {
            var reader = new Reader(filename);
            var processor = new TableDictProcessor();
            var creator = new CreatorFromFile<Dictionary<string, Table>>(reader, processor);
            return creator.Create();
        }
        private EdgeListHolder LoadList(string filename) 
        {
            var reader = new Reader(filename);
            var processor = new EdgeListProcessor();
            processor.PassParameters(NodeTables, EdgeTables);
            var creator = new CreatorFromFile<EdgeListHolder>(reader, processor);
            return creator.Create();
        }


        public void LoadEdgeTables(string filename) => this.EdgeTables = LoadTables(filename);
        public void LoadNodeTables(string filename) => this.NodeTables = LoadTables(filename);

        public void LoadEdgeList(string filename) 
        {
            EdgeListHolder edgeList = LoadList(filename);
            this.vertices = edgeList.vertices;
            this.edges = edgeList.edges;
        }

        public List<Vertex> GetAllVertices() => this.vertices;

    }
}
