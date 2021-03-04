/*! \file 
File includes definition of graph.
Graph contains three lists... inward edges, outgoing edges and vertices.
The base class for nodes and edges is the Element class, each element in a graph has an ID and a table (type).
Also each element knows its position in the list where it is included. 
Graph also contains a list of all labels from a data scheme input file to ensure a quick access to the label type.
 */

using System;
using System.Collections.Generic;

namespace QueryEngine
{

    /// <summary>
    /// The class serves only for holder purpose during creation inside Processor.
    /// It enables us to pass a all the the required graph lists from within one function.
    /// </summary>
    public class EdgeListHolder
    {
        public List<OutEdge> outEdges;
        public List<InEdge> inEdges;

        public EdgeListHolder() {this.outEdges = null; this.inEdges = null; }
    }

    /// <summary>
    /// Graph contains vertices, outgoing edges and inwards edges, also types of elements inside a graph.
    /// Also gives interface to a whole graph.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Nodes schema.
        /// </summary>
        public Dictionary<string, Table> nodeTables;
        /// <summary>
        /// Edges schema.
        /// </summary>
        public Dictionary<string, Table> edgeTables;
        /// <summary>
        /// A map of all properties and their types.
        /// string = Property IRI
        /// Tuple(int,type) = (global property id, type of the property)
        /// </summary>
        public Dictionary<string, Tuple<int, Type>> labels;
        
        public List<Vertex> vertices;
        public List<OutEdge> outEdges;
        public List<InEdge> inEdges;

        /// <summary>
        /// Loads a graph from a files. File names must not be changed.
        /// </summary>
        public Graph()
        {
            this.nodeTables = null;
            this.vertices = null;
            this.outEdges = null;
            this.inEdges = null;
            this.edgeTables = null;
            this.labels = new Dictionary<string, Tuple<int, Type>>();


            Console.WriteLine("Loading tables...");

            this.LoadNodeTables("DataFiles\\NodeTypes.txt");
            this.LoadEdgeTables("DataFiles\\EdgeTypes.txt");
            Console.WriteLine("Loading tables finished.");



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
            Dictionary<string, Table> tables = null;
            try
            {
                var reader = new TableFileReader(filename);
                var processor = new TableDictProcessor();
                processor.PassParameters(labels);
                var creator = new CreatorFromFile<Dictionary<string, Table>>(reader, processor);
                tables = creator.Create();
            } 
            catch (Exception e)
            {
                throw new Exception($"Loading of the tables failed. File: {filename} / Error: {e.GetType()} {e.Message}. Try checking the format of the file.");
            }
            
            if (tables == null || tables.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, tables of the graph are empty. File: {filename}" );
            return tables;

        }
        private void LoadEdgeTables(string filename) => this.edgeTables = LoadTables(filename);
        private void LoadNodeTables(string filename) => this.nodeTables = LoadTables(filename);

        /// <summary>
        /// Loads all vetices from a file.
        /// </summary>
        /// <param name="filename"> File containing data of vertices. </param>
        private void LoadVertices(string filename)
        {
            try
            {
                var reader = new DataFileReader(filename);
                var processor = new VerticesListProcessor();
                processor.PassParameters(nodeTables);
                var creator = new CreatorFromFile<List<Vertex>>(reader, processor);
                this.vertices = creator.Create();
            } 
            catch (Exception e)
            {
                throw new Exception($"Loading of the vertices failed. File: {filename} / Error: {e.GetType()} {e.Message}. Try checking the format of the file and the order of the properties based on the scheme.");
            }

            if (this.vertices == null || this.vertices.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, vertices of the graph are empty. Filename = {filename}");
        }

        /// <summary>
        /// Loads all vetices from a file.
        /// </summary>
        /// <param name="filename"> File containing data of edges. </param>
        private void LoadEdges(string filename)
        {
            try
            {
                var reader = new DataFileReader(filename);
                var processor = new EdgeListProcessor();
                processor.PassParameters(edgeTables, vertices);
                var creator = new CreatorFromFile<EdgeListHolder>(reader, processor);
                var result = creator.Create();
                this.outEdges = result.outEdges;
                this.inEdges = result.inEdges;
            }
            catch (Exception e)
            {
                throw new Exception($"Loading of the edges failed. File: {filename} / Error: {e.GetType()} {e.Message}. Try checking the format of the file and the order of the properties based on the scheme.");
            }

            if (this.outEdges == null || this.outEdges.Count == 0) 
                throw new ArgumentException($"{this.GetType()} Out edges of the graph are empty. Filename = {filename}");
            if (this.inEdges == null || this.inEdges.Count == 0) 
                throw new ArgumentException($"{this.GetType()} In edges of the graph are empty. Filename = {filename}");

        }

        public List<Vertex> GetAllVertices() => this.vertices;
        public List<OutEdge> GetAllOutEdges() => this.outEdges;
        public List<InEdge> GetAllInEdges() => this.inEdges;

    }
}
