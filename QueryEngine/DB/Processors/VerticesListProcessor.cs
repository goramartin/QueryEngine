/*! \file 
  File includes definition of vertices list processor.

  Processor creates a list of vertices. The edges position for each vertex are not filled.
  Processor expects the vertices to have a unique id, preferably sorted by ascending order.
  The unput file should look like: ID TYPE PROPERTIES.
  Properties are set to a table defined by a type and ID is a unique identifier in the entire graph.
  Hence the ID is not direcly a property of the element.

  States of a processor are singletons and flyweight since they do not encompass any additional varibales.

 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Creates vertices list from a file.
    /// </summary>
    class VerticesListProcessor : IProcessor<List<Vertex>>
    {
        IProcessorState<List<Vertex>> processorState { get; set; }
        List<Vertex> vertices;
        Dictionary<string, Table> nodeTables;
        Vertex vertex;
        int paramsToReadLeft;
        bool finished;


        public VerticesListProcessor()
        {
            this.vertices = new List<Vertex>();
            this.processorState = NodeIDState.Instance;
            this.paramsToReadLeft = 0;
            this.finished = false;
        }

        public bool Finished()
        {
            return this.finished;
        }

        public List<Vertex> GetResult()
        {
            return this.vertices;
        }

        public void PassParameters(params object[] prms)
        {
            this.nodeTables = (Dictionary<string, Table>)prms[0];
        }

        public void Process(string param)
        {
            if (!this.finished) this.processorState.Process(this, param);
        }

        public void SetNewState(IProcessorState<List<Vertex>> state)
        {
            this.processorState = state;
        }

        /// <summary>
        /// First state of processor. Tries to parse ID of a node and inits a new vertex.
        /// After parsing ID, the type of node is a next state.
        /// </summary>
        class NodeIDState : IProcessorState<List<Vertex>>
        {
            static NodeIDState instance =
             new NodeIDState();

            private NodeIDState() { }

            public static NodeIDState Instance
            {
                get  {   return instance;  }
            }

            public void Process(IProcessor<List<Vertex>> processor, string param)
            {
                var proc = (VerticesListProcessor)processor;

                if (param == null)
                {
                    proc.finished = true;
                    return;
                };

                int id = 0;
                if (!int.TryParse(param, out id))
                    throw new ArgumentException($"{this.GetType()}, reading wrong node ID. ID is not a number. ID = {param}");

                proc.vertex = new Vertex();
                proc.vertex.PositionInList = proc.vertices.Count;
                proc.vertex.AddID(id);
                proc.SetNewState(NodeTypeState.Instance);
            }
        }

        /// <summary>
        /// Mid state between end of parameters reading. Serves only as a method implementation FinishParams for children.
        /// If reading of parameters of the node was finished next state if ID, that is reading a new node.
        /// Otherwise, we continue reading next parameters.
        /// </summary>
        abstract class NodeParamsEndState : IProcessorState<List<Vertex>>
        {
            public abstract void Process(IProcessor<List<Vertex>> processor, string param);

            protected void FinishParams(IProcessor<List<Vertex>> processor)
            {
                var proc = (VerticesListProcessor)processor;

                // For no more parameters to parse left
                if (proc.paramsToReadLeft == 0)
                {
                    proc.vertices.Add(proc.vertex);
                    proc.SetNewState(NodeIDState.Instance);
                }
                // Continue parsing parameters
                else proc.SetNewState(NodeParametersState.Instance);
            }
        }


        /// <summary>
        /// Finds table based on a parameter and set it to a node.
        /// Also inserts ID of the node into the table.
        /// Next state should parse data of the node.
        /// </summary>
        class NodeTypeState : NodeParamsEndState
        {
            static NodeTypeState instance =
             new NodeTypeState();

            private NodeTypeState() { }

            public static NodeTypeState Instance
            {
                get { return instance; }
            }

            public override void Process(IProcessor<List<Vertex>> processor, string param)
            {
                var proc = (VerticesListProcessor)processor;

                Table table;
                proc.nodeTables.TryGetValue(param, out table);
                proc.vertex.AddTable(table);
                proc.vertex.Table.AddID(proc.vertex.ID);

                proc.paramsToReadLeft = proc.vertex.Table.GetPropertyCount();
                FinishParams(processor);
            }
        }

        /// <summary>
        /// Gets position of accessed property and parses its value to its list.
        /// </summary>
        class NodeParametersState : NodeParamsEndState
        {
            static NodeParametersState instance =
             new NodeParametersState();

            private NodeParametersState() { }

            public static NodeParametersState Instance
            {
                get { return instance; }
            }

            public override void Process(IProcessor<List<Vertex>> processor, string param)
            {
                var proc = (VerticesListProcessor)processor;

                // Get position of accessed property and insert given parameter to appropriate list.
                int accessedPropertyPosition = proc.vertex.Table.GetPropertyCount() - proc.paramsToReadLeft;
                proc.vertex.Table.Properties[accessedPropertyPosition].ParsePropFromStringToList(param);

                proc.paramsToReadLeft--;
                FinishParams(proc);
            }
        }
    }
}
