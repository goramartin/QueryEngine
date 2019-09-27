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
        public int positionInList;

        public void AddID(int id) => this.id = id;
        public void AddTable(Table table) => this.table = table;

        public Table GetTable() => this.table;
        public int GetID() => this.id;
    }


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

    class Edge : Element
    {
        public Vertex endVertex;

        public Edge(int id, Table table, Vertex vertex)
        {
            this.id = id;
            this.table = table;
            this.endVertex = vertex;
            this.positionInList = -1;
        }
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
    }

    //Only for holder purpose during creation inside Processor
    class EdgeListHolder
    {
        public List<Vertex> vertices;
        public List<Edge> outEdges;
        public List<Edge> inEdges;

        public EdgeListHolder() { this.vertices = null; this.outEdges = null; this.inEdges = null; }
    }

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

        private Dictionary<string, Table> LoadTables(string filename)
        {
            var reader = new Reader(filename);
            var processor = new TableDictProcessor();
            var creator = new CreatorFromFile<Dictionary<string, Table>>(reader, processor);
            return creator.Create();
        }
        private EdgeListHolder LoadList(string filename) 
        {
            var reader = new WordReader(filename);
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
            this.outEdges = edgeList.outEdges;
            this.inEdges = edgeList.inEdges;
        }

        public List<Vertex> GetAllVertices() => this.vertices;
        public List<Edge> GetAllOutEdges() => this.outEdges;
        public List<Edge> GetAllInEdges() => this.inEdges;

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
