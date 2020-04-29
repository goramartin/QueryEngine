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
        EdgeListHolder holder = new EdgeListHolder();

        List<Vertex> vertices;
        List<OutEdge> outEdges;
        List<InEdge> inEdges;

        Dictionary<string, Table> edgeTables;
        /// <summary>
        /// Each vertex has a set of inwards edges
        /// </summary>
        List<InEdge>[] incomingEdgesTable; 

        bool finished;
        InEdge incomingEdge;
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
            InicialiseInEdgesTables();
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


                if (param == null) { proc.FinalizeInEdges(); proc.FinalizeVertices(); proc.finished = true; return; }

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
                proc.outEdge.AddTable(table);
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
                proc.incomingEdge = new InEdge();
                proc.incomingEdge.EndVertex = fromVertex;
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

                proc.incomingEdge.AddTable(proc.outEdge.Table);
                proc.incomingEdge.AddID(proc.outEdge.ID);
                proc.incomingEdgesTable[endVertex.PositionInList].Add(proc.incomingEdge);

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
        ///Merge results from inedges tables into one. -> Creates inEdges list.
        ///Set positions for inEdges field in vertices.
        ///Set positions for inEdges in their own list.
        /// </summary>
        private void FinalizeInEdges()
        {
            int count = 0;

            for (int i = 0; i < incomingEdgesTable.Length; i++)
            {
                int c = incomingEdgesTable[i].Count;
                if (c == 0) continue;
                vertices[i].InEdgesStartPosition = count;
                for (int k = 0; k < c; k++)
                    inEdges.Add(incomingEdgesTable[i][k]);
                count += c;
            }

            SetPositionsInListforInEdges();
        }



        /// <summary>
        /// For each edge from in edges, set its position in a list.
        /// </summary>
        private void SetPositionsInListforInEdges()
        {
            for (int i = 0; i < inEdges.Count; i++)
            {
                inEdges[i].PositionInList = i;
            }
        }


        /// <summary>
        /// Creates list for each vertex. Each table will include incoming edges.
        /// </summary>
        private void InicialiseInEdgesTables()
        {
            this.incomingEdgesTable = new List<InEdge>[vertices.Count];
            for (int i = 0; i < incomingEdgesTable.Length; i++)
            {
                incomingEdgesTable[i] = new List<InEdge>();
            }
        }

        /// <summary>
        /// Set count on in/out edges.
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
        /// Based on a given vertex we set ending positions of in/out edges in their lists.
        /// </summary>
        /// <param name="isOut"> Wheter we are setting in or out edges.</param>
        /// <param name="p"> Position of a processed vertex.</param>
        /// <returns></returns>
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
