using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    abstract class Field
    {
        public int id;
        public Table table;

        public void AddID(int id) => this.id = id;
        public void AddTable(Table table) => this.table = table;
    }


    class Vertex : Field
    {

        public int edgePosition;
        public List<IncomingEdge> incomingEdges;
        public Vertex(int id, Table table)
        {
            this.id = id;
            this.table = table;
            this.edgePosition = -1;
            this.incomingEdges = new List<IncomingEdge>();
        }

        public Vertex()
        {
            this.id = -1;
            this.table = null;
            this.edgePosition = -1;
            this.incomingEdges = new List<IncomingEdge>();

        }

        public void SetEdgePosition(int position) => this.edgePosition = position;

        public bool HasEdges() { if (this.edgePosition == -1) return false; else return true; }

        public void AddIncomingEdge(IncomingEdge e) { this.incomingEdges.Add(e); }

    }


    class IncomingEdge
    {
        public Vertex FromVertex;
        public Edge incomingEdge;

        public IncomingEdge()
        {
            this.FromVertex = null;
            this.incomingEdge = null;
        } 

        public void AddFromVertex(Vertex v) { this.FromVertex = v; }
        public void AddEdge(Edge e) { this.incomingEdge = e; }
    }
    class Edge:Field
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



    }
}
