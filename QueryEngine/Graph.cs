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
        public int outEdgePosition;
        public int inEdgePosition;
        public Vertex(int id, Table table)
        {
            this.id = id;
            this.table = table;
            this.outEdgePosition = -1;
            this.inEdgePosition = -1;
            this.positionInList = -1;
        }

        public Vertex()
        {
            this.id = -1;
            this.table = null;
            this.outEdgePosition = -1;
            this.inEdgePosition = - 1;

        }

        public void SetPositionInVertices(int position) => this.positionInList = position;
        public void SetOutEdgePosition(int position) => this.outEdgePosition = position;
        public void SetInEdgePosition(int position) => this.inEdgePosition = position;

        public bool HasEdges() { if (this.outEdgePosition == -1) return false; else return true; }
        public int GetOutEdgePosition() => this.outEdgePosition;
        public int GetInEdgePosition() => this.inEdgePosition;
        public int GetPositionInVertices() => this.positionInList;

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

        public int GetPositionOfEdges(bool isOut, int positionOfVertex)
        {
            if (isOut) return vertices[positionOfVertex].outEdgePosition;
            else return vertices[positionOfVertex].inEdgePosition;
        }

        public int GetRangeToLastEdgeOfVertex(bool isOut, int positionOfVertex)
        {
            //Has edge?
            if (GetPositionOfEdges(isOut, positionOfVertex) == -1) return -1;
          
            //Find first vertex that has edges and return start of those edges.
            for (int i = positionOfVertex + 1; i < vertices.Count; i++)
            {
                int t = GetPositionOfEdges(isOut, i);
                if (t != -1) return t;
            }
            //Else the edges of the vertex on positionofvertex continue until end of array.
            if (isOut) return outEdges.Count;
            else return inEdges.Count;
            

        }
    }
}
