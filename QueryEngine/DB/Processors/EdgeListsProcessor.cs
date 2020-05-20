/*! \file 
  File includes definition of Edge list processor.

  Processor creates lists of in edges and out edges, the return value is a holder for both lists.
  Firstly, the in edges defined in a file are read. It is expected that the edges are sorted based
  on the vertices input file in the same ascending order. That means if the first element is a vertex with id 1
  the edges in the file are edges with the starting vertex of the id 1 and vice versa.

  The file is expected to look like: ID TYPE FROMID TOID PROPERTIES
  Type defines the table of the edge. And from id and to id forms the starting vertex id and the end vertex id.
  Properties are inputed to the table defined in the TYPE.

  States are singletons and flyweight since they do not contain any additional variables.
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Creates edge lists from data file.
    /// We suppose vertices in datafile are stored based on their id in ascending order.
    /// We suppose edges in datafile are stored based on id of the from vertex in ascending order.
    /// That is to say, having three vertices with ids 1, 2, 3... first all edges are from vertex 1, then edges from vertex 2 etc. 
    /// </summary>
    sealed class EdgeListProcessor : IProcessor<EdgeListHolder>
    {
        IProcessorState<EdgeListHolder> processorState;
        List<Vertex> vertices;
        List<OutEdge> outEdges;
        List<InEdge> inEdges;

        Dictionary<string, Table> edgeTables;
        bool finished;
        OutEdge outEdge;
        int paramsToReadLeft;

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
            var tmp = new EdgeListHolder();
            tmp.outEdges = this.outEdges;
            tmp.inEdges = this.inEdges;
            return tmp;
        }

        public void PassParameters(params object[] prms)
        {
            this.edgeTables = (Dictionary<string, Table>)prms[1];
            this.vertices = (List<Vertex>)prms[2];
        }

        /// <summary>
        /// A jump table which defines what method will be called in a given state.
        /// </summary>
        /// <param name="param"> Parameter to process. </param>
        public void Process(string param)
        {
           if (!finished) this.processorState.Process(this, param);
        }

        public void SetNewState(IProcessorState<EdgeListHolder> state)
        {
            this.processorState = state;
        }

        /// <summary>
        /// Processes id of an edge.
        /// Creates new outgoing edge. Next state is processing of a type.
        /// </summary>
        sealed class EdgeIDState : IProcessorState<EdgeListHolder>
        {
            static EdgeIDState instance =
             new EdgeIDState();

            private EdgeIDState() { }

            public static EdgeIDState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<EdgeListHolder> processor, string param)
            {
                var proc = (EdgeListProcessor)processor;


                if (param == null) { 
                    proc.FinalizeEdgesEndPositionInVertices(isOut: true);
                    proc.CreateInEdges();
                    proc.FinalizeEdgesEndPositionInVertices(isOut: false);
                    proc.finished = true;
                    return;
                }

                int id = 0;
                if (!int.TryParse(param, out id))
                    throw new ArgumentException($"{this.GetType()}, reading wrong node ID. ID is not a number. ID = {param}");

                proc.outEdge = new OutEdge();
                proc.outEdge.PositionInList = proc.outEdges.Count;
                proc.outEdge.ID = id;
                proc.SetNewState(EdgeTypeState.Instance);
            }
        }

        /// <summary>
        /// Finds table assiciated with the edge and inserts the edge inside.
        /// Also sets the table for the edge.
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
                 
                Table table;
                proc.edgeTables.TryGetValue(param, out table);
                proc.outEdge.Table = table;
                proc.outEdge.Table.AddID(proc.outEdge.ID);
                proc.SetNewState(EdgeFromIDState.Instance);
            }
        }

        /// <summary>
        /// Find vertex the edge starts from. If edge processed is first edge of vertex, set edge position.
        /// Note the Count is pointing to the empty space where the processed edge will be added in FinishParams.
        /// Also sets values to the opposite edge.
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
                proc.SetNewState(EdgeToIDState.Instance);
            }
        }


        /// <summary>
        /// Mid state between end of parameters reading. Serves only as a method implementation FinishParams for children.
        /// If reading of parameters of the node was finished next state if ID, that is reading a new node.
        /// Otherwise, we continue reading next parameters.
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
                    proc.outEdges.Add(proc.outEdge);
                    proc.SetNewState(EdgeIDState.Instance);
                }
                //continue parsing parameters
                else proc.SetNewState(EdgeParameterState.Instance);
            }
        }

        /// <summary>
        /// Finds end vertex of an edge and sets him to the end position of out edge.
        /// Finishes processing of in edge and adds it to the appropriate table of the vertex.
        /// </summary>
        sealed class EdgeToIDState: EdgeParamsEndState
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
                proc.paramsToReadLeft = proc.outEdge.Table.GetPropertyCount();
                FinishParams(proc);
            }
        }

        /// <summary>
        ///Get the position of property where adding the parameter.
        ///Add the parameter there.
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
                int accessedPropertyPosition = proc.outEdge.Table.GetPropertyCount() - proc.paramsToReadLeft;
                proc.outEdge.Table.Properties[accessedPropertyPosition].ParsePropFromStringToList(param);
                proc.paramsToReadLeft--;
                FinishParams(proc);
            }
        }


        /// <summary>
        /// Finds vertex in a list based on a given ID.
        /// </summary>
        /// <param name="param"> ID of a vertex to be found</param>
        /// <returns> Vertex with given parameter.</returns>
        private Vertex FindVertex(string param)
        {
            int id = 0;
            if (!int.TryParse(param, out id))
                throw new ArgumentException($"{this.GetType()}, reading wrong node ID. ID is not a number. ID = {param}");
            Vertex vertex = this.vertices.Find(x => x.ID == id);
            if (vertex == null) throw new ArgumentException($"{this.GetType()}, ID is not found in vertices. ID = {id}");
            return vertex;
        }

        /// <summary>
        /// Creates in edges.
        /// For each vertex in the graph, find edges that points to that vertex,
        /// and create an in edge for this vertex.
        /// </summary>
        private void CreateInEdges()
        {
            // For each vertex in the graph...
            foreach (Vertex processedVertex in this.vertices)
            {
                // ... iterate over each vertex in the graph ...
                for (int vertexIndex = 0; vertexIndex < this.vertices.Count; vertexIndex++)
                {
                    // ... if the vertex has out edges, iterate over its edges ... 
                    if (this.vertices[vertexIndex].HasOutEdges())
                    {
                        for (int edgeIndex = this.vertices[vertexIndex].OutEdgesStartPosition;
                             edgeIndex < this.vertices[vertexIndex].OutEdgesEndPosition; edgeIndex++)
                        {
                            // ... if the edge points to the processed vertex, 
                            // then create new InEdge for the processed vertex.
                            if (this.outEdges[edgeIndex].EndVertex.ID == processedVertex.ID)
                            {
                                var tmpInEdge = new InEdge();
                                tmpInEdge.ID = this.outEdges[edgeIndex].ID;
                                tmpInEdge.Table =this.outEdges[edgeIndex].Table;
                                tmpInEdge.PositionInList = this.inEdges.Count; 
                                tmpInEdge.EndVertex = this.vertices[vertexIndex];

                                if (!processedVertex.HasInEdges()) 
                                    processedVertex.InEdgesStartPosition = tmpInEdge.PositionInList;
                                this.inEdges.Add(tmpInEdge);
                            }
                        }
                    }
                    else continue;
                }
            }
        }

        /// <summary>
        /// Set end positions for edges of a vertex.
        /// </summary>
        /// <param name="isOut"> A parameter whether to finilaze position of in edges or out edges. </param>
        private void FinalizeEdgesEndPositionInVertices(bool isOut)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                if (isOut) vertices[i].OutEdgesEndPosition = FindEndPositionOfEdges(isOut: true, i);
                else vertices[i].InEdgesEndPosition = FindEndPositionOfEdges(isOut: false, i);
            }
        }

        /// <summary>
        /// Based on a given vertex we set ending positions of in/out edges in their lists.
        /// </summary>
        /// <param name="isOut"> Wheter we are setting in or out edges.</param>
        /// <param name="p"> Position of a processed vertex.</param>
        /// <returns> End position of edges of a vertex on a given position.
        /// -1 if the vertex does not have a edges. Otherwise returns 
        ///  index of the first edge that does not belong to the given vertex.
        ///  </returns>
        private int FindEndPositionOfEdges(bool isOut, int p)
        {
            // if the vertex had no edges of the given type
            if ((isOut) && (!vertices[p].HasOutEdges())) return -1;
            else if ((!isOut) && (!vertices[p].HasInEdges())) return -1;

            // From the given position of a vertex, iterate over subsequent vertices and search for start of their edges
            for (int k = p + 1; k < vertices.Count; k++)
            {
                Vertex v = vertices[k];
                int t = isOut ? v.OutEdgesStartPosition : v.InEdgesStartPosition;
                if (t != -1) return t;
            }

            // if it reaches end of the vertices list, that means the ending of edges is at the end of the edge list
            if (isOut) return outEdges.Count;
            else return inEdges.Count;
        }
    }
}
