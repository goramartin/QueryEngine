/*! \file 
The file includes a definition of an edge List processor.

The processor creates Lists of in edges and out edges, the return value is a holder for both Lists.
Firstly, the in edges defined in a file are read. It is expected that the edges are sorted based
on the vertices IDs in the same ascending order. That means, if the first element is a vertex with id 1
the edges in the file are edges with the starting vertex with the id 1 and vice versa.

The file is expected to look like: ID TYPE FROMID TOID (PROPERTIES)*
TYPE defines the table of the edge.
FROMID and TOID forms the starting vertex id and the end vertex id.
PROPERTIES are inputed to the table defined in the TYPE.

States are singletons and flyweight since they do not contain any additional variables.
 */

using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Creates edge Lists from data file.
    /// We suppose vertices in a datafile are stored based on their id in ascending order.
    /// We suppose edges in datafile are stored based on id of the FROMID vertex in ascending order.
    /// That is to say, having three vertices with ids 1, 2, 3... first all edges are from vertex 1, then edges from vertex 2 etc...
    /// More about creation is written at the state classes.
    /// Note that during parsing of an out edge, simultaneously the reverse edge is created (in edge).
    /// Note that each vertex has its own bin for in edges. The bins are then emptied to the final in edge List.
    /// The bins are used because otherwise the assignment of in edges takes way too much time even on a small graph.
    /// </summary>
    internal sealed class EdgeListProcessor : IProcessor<EdgeListHolder>
    {
        private IProcessorState<EdgeListHolder> processorState;
        private Dictionary<string, Table> edgeTables;
        private List<Vertex> vertices;
        private List<OutEdge> outEdges;
        private List<InEdge> inEdges;
        /// <summary>
        /// An index of vertices IDs to a position in their List. To speed up loading of edges.
        /// (vertex ID, position)
        /// </summary>
        private Dictionary<int, int> verticesIndex;
        /// <summary>
        /// Each vertex has a set of inwards edges
        /// </summary>
        private List<InEdge>[] incomingEdgesTable;

        private bool finished;
        private InEdge incomingEdge;
        private OutEdge outEdge;
        private int paramsToReadLeft;

        public EdgeListProcessor()
        {
            this.outEdges = new List<OutEdge>();
            this.inEdges = new List<InEdge>();
            this.finished = false;
            this.processorState = EdgeIDState.Instance;
            this.paramsToReadLeft = 0;
        }

        public bool Finished()
        {
            return this.finished;
        }

        public EdgeListHolder GetResult()
        {
            var tmp = new EdgeListHolder
            {
                outEdges = this.outEdges,
                inEdges = this.inEdges
            };
            return tmp;
        }

        public void PassParameters(params object[] prms)
        {
            this.edgeTables = (Dictionary<string, Table>)prms[0];
            this.vertices = (List<Vertex>)prms[1];
            InicialiseInEdgesTables();
            CreateVerticesIndex();
        }

        /// <summary>
        /// The method creates an index from the vertices IDs to speed up 
        /// adding of new edges to appropriate vertices.
        /// </summary>
        private void CreateVerticesIndex()
        {
            this.verticesIndex = new Dictionary<int, int>();
            for (int i = 0; i < this.vertices.Count; i++)
            {
                if (this.verticesIndex.TryGetValue(this.vertices[i].ID, out int pos))
                    throw new Exception($"Two Vertices have the same ID = {this.vertices[i].ID}. Adjust the input data files.");
                else this.verticesIndex.Add(this.vertices[i].ID, this.vertices[i].PositionInList);
            }
        }

        /// <summary>
        /// A jump method which defines what method will be called in a given state.
        /// </summary>
        /// <param name="param"> A parameter to process. </param>
        public void Process(string param)
        {
            if (!finished) this.processorState.Process(this, param);
        }

        public void SetNewState(IProcessorState<EdgeListHolder> state)
        {
            this.processorState = state;
        }

        /// <summary>
        /// Processes an id of an edge.
        /// Creates a new outgoing edge. The next state is processing of a type.
        /// New edges are added to the end of the List containing out edges.
        /// </summary>
        sealed class EdgeIDState : IProcessorState<EdgeListHolder>
        {
            static EdgeIDState instance =
             new EdgeIDState();

            int count;
            private EdgeIDState() { }

            public static EdgeIDState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<EdgeListHolder> processor, string param)
            {
                var proc = (EdgeListProcessor)processor;

                // Just a test print.
                count++;
                if (count % 200000 == 0) Console.WriteLine(count);

                if (param == null) 
                {
                    proc.FinalizeInEdges();
                    proc.FinalizeVertices();
                    proc.finished = true;
                    return; 
                }

                // Create a new out edge.
                if (!int.TryParse(param, out int id))
                    throw new ArgumentException($"{this.GetType()}, reading wrong node ID. ID is not a number. ID = {param}");

                proc.outEdge = new OutEdge
                {
                    PositionInList = proc.outEdges.Count,
                    ID = id
                };

                // The next state is a parsing of a TYPE.
                proc.SetNewState(EdgeTypeState.Instance);
            }
        }

        /// <summary>
        /// Finds a table assiciated with the out edge and inserts the out edge inside.
        /// Also sets the table for the out edge.
        /// </summary>
        sealed class EdgeTypeState : IProcessorState<EdgeListHolder>
        {
            static EdgeTypeState instance =
             new EdgeTypeState();

            private EdgeTypeState() { }

            public static EdgeTypeState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<EdgeListHolder> processor, string param)
            {
                var proc = (EdgeListProcessor)processor;

                // Find the table.
                proc.edgeTables.TryGetValue(param, out Table table);
                proc.outEdge.Table = table;

                // Add the edge id to the found table.
                proc.outEdge.Table.AddID(proc.outEdge.ID);

                // The next state is a parsing of a FROMID
                proc.SetNewState(EdgeFromIDState.Instance);
            }
        }

        /// <summary>
        /// Find a vertex the out edge starts from. If the out edge being processed is the first
        /// out edge of the vertex, set a starting edge position of out edges for the vertex.
        /// Start creation of a reverse edge to the one being read.
        /// </summary>
        sealed class EdgeFromIDState : IProcessorState<EdgeListHolder>
        {
            static EdgeFromIDState instance =
             new EdgeFromIDState();

            private EdgeFromIDState() { }

            public static EdgeFromIDState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<EdgeListHolder> processor, string param)
            {
                var proc = (EdgeListProcessor)processor;

                Vertex fromVertex = proc.FindVertex(param);
                if (!fromVertex.HasOutEdges()) fromVertex.OutEdgesStartPosition = proc.outEdges.Count;

                proc.incomingEdge = new InEdge
                {
                    EndVertex = fromVertex
                };

                // The next state is a parsing of a TOID
                proc.SetNewState(EdgeToIDState.Instance);
            }
        }


        /// <summary>
        /// This class provides a method for finishing reading of parameters of the out edge.
        /// If reading of parameters of the node was finished then the next state is parsing of an ID, that is reading a new edge.
        /// Otherwise, we continue reading more parameters pertaining to the out edge.
        /// </summary>
        abstract class EdgeParamsEndState : IProcessorState<EdgeListHolder>
        {
            public abstract void Process(IProcessor<EdgeListHolder> processor, string param);

            protected void FinishParams(IProcessor<EdgeListHolder> processor)
            {
                var proc = (EdgeListProcessor)processor;

                //For no more parameters to parse left
                if (proc.paramsToReadLeft == 0)
                {
                    // Add the completed out edge to the end of out edges List. 
                    proc.outEdges.Add(proc.outEdge);
                    // The next step is to try reading a new out edge.
                    proc.SetNewState(EdgeIDState.Instance);
                }
                // Continue parsing parameters
                else proc.SetNewState(EdgeParameterState.Instance);
            }
        }

        /// <summary>
        /// Finds the end vertex of the out edge.
        /// Finishes processing of in edge and adds it to the appropriate table of the vertex.
        /// Start reading parameters of the out edge.
        /// </summary>
        sealed class EdgeToIDState : EdgeParamsEndState
        {
            static EdgeToIDState instance =
             new EdgeToIDState();

            private EdgeToIDState() { }

            public static EdgeToIDState Instance
            {
                get { return instance; }
            }

            public override void Process(IProcessor<EdgeListHolder> processor, string param)
            {
                var proc = (EdgeListProcessor)processor;

                Vertex endVertex = proc.FindVertex(param);
                proc.outEdge.EndVertex = endVertex;

                // Finish the creation of the reverse edge.
                proc.incomingEdge.Table = proc.outEdge.Table;
                proc.incomingEdge.ID = proc.outEdge.ID;
                proc.incomingEdgesTable[endVertex.PositionInList].Add(proc.incomingEdge);

                // Start reading properties of the out edge.
                // This indicates the number of parameters that the out edge has.
                proc.paramsToReadLeft = proc.outEdge.Table.PropertyCount;
                FinishParams(proc);
            }
        }

        /// <summary>
        /// The property of the out edge is expected.
        /// Get the position of the property where adding the passed parameter.
        /// Add the parameter there and try to read another property.
        /// </summary>
        sealed class EdgeParameterState : EdgeParamsEndState
        {
            static EdgeParameterState instance =
             new EdgeParameterState();

            private EdgeParameterState() { }

            public static EdgeParameterState Instance
            {
                get { return instance; }
            }

            public override void Process(IProcessor<EdgeListHolder> processor, string param)
            {
                var proc = (EdgeListProcessor)processor;
                var tmpTable = proc.outEdge.Table;

                // Get the position of a property inside the table of the out edge. 
                int accessedPropertyPosition = tmpTable.PropertyCount - proc.paramsToReadLeft;

                // Parse the value from parameter.
                tmpTable.Properties[tmpTable.PropertyLabels[accessedPropertyPosition]].ParsePropFromStringToList(param);
                
                // Try to read another property.
                proc.paramsToReadLeft--;
                FinishParams(proc);
            }
        }



        /// <summary>
        /// Finds a vertex in a List based on a given ID.
        /// </summary>
        /// <param name="param"> An ID of a vertex to be found. </param>
        /// <returns> The vertex with given parameter. </returns>
        private Vertex FindVertex(string param)
        {
            if (!int.TryParse(param, out int id))
                throw new ArgumentException($"{this.GetType()}, reading wrong node ID. ID is not a number. ID = {param}");
            else if (!this.verticesIndex.TryGetValue(id, out int value))
                throw new ArgumentException($"{this.GetType()}, could not find corresponding ID to a vertex. ID = {id}");
            else return this.vertices[value];
        }


        /// <summary>
        /// Merge results from inedges tables into one. -> Creates an in edges List.
        /// Set positions for the in edges field in vertices. (start and end of a range)
        /// Set positions for the in edges in their own List.
        /// </summary>
        private void FinalizeInEdges()
        {
            int count = 0;

            // An iteration over all vertices in the graph.
            for (int i = 0; i < incomingEdgesTable.Length; i++)
            {
                int c = incomingEdgesTable[i].Count;
                
                // If there are no in edges, do not set the position to the vertex.
                // The position will be set to -1.
                if (c == 0) continue;

                // Otherwise, set the position.
                // And add them to the in edge List.
                vertices[i].InEdgesStartPosition = count;
                for (int k = 0; k < c; k++)
                    inEdges.Add(incomingEdgesTable[i][k]);
                count += c;
            }

            SetPositionsInListforInEdges();
        }

        /// <summary>
        /// For each edge from in edges, set its position in a List.
        /// </summary>
        private void SetPositionsInListforInEdges()
        {
            for (int i = 0; i < inEdges.Count; i++)
                inEdges[i].PositionInList = i;
        }


        /// <summary>
        /// Creates a List for each vertex. Each table will include incoming edges.
        /// To these Lists the in edges will be added.
        /// </summary>
        private void InicialiseInEdgesTables()
        {
            this.incomingEdgesTable = new List<InEdge>[vertices.Count];
            for (int i = 0; i < incomingEdgesTable.Length; i++)
                incomingEdgesTable[i] = new List<InEdge>();
        }

        /// <summary>
        /// Set end positions for a vertex for its in/out edges.
        /// </summary>
        private void FinalizeVertices()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].OutEdgesEndPosition = FindEndPositionOfEdges(isOut: true, i);
                vertices[i].InEdgesEndPosition = FindEndPositionOfEdges(isOut: false, i);
            }
        }


        /// <summary>
        /// Based on a given vertex set ending positions of in/out edges in their Lists.
        /// </summary>
        /// <param name="isOut"> Wheter to set in or out edges.</param>
        /// <param name="p"> A position of a processed vertex.</param>
        /// <returns> The end position of in/out edges for the given vertex. </returns>
        private int FindEndPositionOfEdges(bool isOut, int p)
        {
            if ((isOut) && (!vertices[p].HasOutEdges())) return -1;
            else if ((!isOut) && (!vertices[p].HasInEdges())) return -1;

            for (int k = p + 1; k < vertices.Count; k++)
            {
                Vertex v = vertices[k];
                int t = isOut ? v.OutEdgesStartPosition : v.InEdgesStartPosition;
                if (t != -1) return t;
            }

            if (isOut) return outEdges.Count;
            else return inEdges.Count;
        }
    }
}
