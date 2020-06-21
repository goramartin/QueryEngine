/*! \file 
  File includes definition of graph.
  Graph contains three lists... inward edges, outgoing edges and vertices.
  Base class for nodes and edges is Element class, each element in a graph has got an ID and
  a table (type). Also each element knows its position in the list where it is included. 
  
  Each vertex has got a positions for edges in edge lists, one positions for incoming edges and 
  one for outgoing edges. Starting position means that on that position the edge from this vertex is leading and
  end position means that on that position, edges from a consecutive vertex are starting.
 
  Graph also contains a list of all labels from a data scheme input file to ensure a quick access to the label type.

 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

   
    /// <summary>
    /// The class serves only for holder purpose during creation inside Processor.
    /// It enables us to pass a all the the required graph lists from withing one function.
    /// </summary>
    internal class EdgeListHolder
    {
        public List<OutEdge> outEdges;
        public List<InEdge> inEdges;

        public EdgeListHolder() {this.outEdges = null; this.inEdges = null; }
    }


    /// <summary>
    /// Graph contains vertices, outgoing edges and inwards edges, also types of elements inside a graph.
    /// Also gives interface to a whole graph.
    /// </summary>
    internal class Graph
    {
        public Dictionary<string, Table> NodeTables;
        public Dictionary<string, Table> EdgeTables;
        public Dictionary<string, Type> Labels;
        public List<Vertex> vertices;
        public List<OutEdge> outEdges;
        public List<InEdge> inEdges;

        /// <summary>
        /// Loads a graph from a files. Files should not be changed.
        /// </summary>
        public Graph()
        {
            this.NodeTables = null;
            this.vertices = null;
            this.outEdges = null;
            this.inEdges = null;
            this.EdgeTables = null;


            Console.WriteLine("Loading tables...");
            this.LoadNodeTables("DataFiles\\NodeTypes.txt");
            this.LoadEdgeTables("DataFiles\\EdgeTypes.txt");
            Console.WriteLine("Loading tables finished.");


            this.Labels = new Dictionary<string, Type>();
            this.AdjustLabels(this.EdgeTables);
            this.AdjustLabels(this.NodeTables);

            Console.WriteLine("Loading nodes...");
            this.LoadVertices("DataFiles\\Nodes.txt");
            Console.WriteLine("Loading nodes finished.");


            Console.WriteLine("Loading edges...");
            this.LoadEdges("DataFiles\\Edges.txt");
            Console.WriteLine("Loading edges finished.");
        }

        /// <summary>
        /// Loads table types from a file. 
        /// </summary>
        /// <param name="filename"> A file containing definitions of tables. </param>
        /// <returns> Dictionary of tables. </returns>
        private Dictionary<string, Table> LoadTables(string filename)
        {
            var reader = new TableFileReader(filename);
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

        /// <summary>
        /// Adjust labels from all tables to a graph dictionary (Labels).
        /// </summary>
        /// <param name="tables"> Dictionaty of graph types. </param>
        private void AdjustLabels(Dictionary<string, Table> tables)
        {
            foreach (var item in tables)
            {
                this.AddTableLabels(item.Value);
            }
        }


        /// <summary>
        /// Fills missing labels from a table to graph dictionary (Labels).
        /// </summary>
        /// <param name="table"> Table to take properties from. </param>
        private void AddTableLabels(Table table)
        {
            for (int i = 0; i < table.Properties.Count; i++)
            {
                if (this.Labels.TryGetValue(table.Properties[i].IRI, out Type type))
                {
                    if (type != table.Properties[i].GetPropertyType())
                        throw new ArgumentException($"{this.GetType()}, found two properties with the same name but discrepant types. Adjust input scheme.");
                }
                else this.Labels.Add(table.Properties[i].IRI, table.Properties[i].GetPropertyType());
            }
        }

        public List<Vertex> GetAllVertices() => this.vertices;
        public List<OutEdge> GetAllOutEdges() => this.outEdges;
        public List<InEdge> GetAllInEdges() => this.inEdges;

    }
}
