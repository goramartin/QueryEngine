/*! \file 
File includes definition of vertices list processor.

Processor creates a list of vertices. The edges position for each vertex are not filled (set to -1).
Processor expects the vertices to have a unique id, preferably sorted by ascending order.

The input file should look like: ID TYPE PROPERTIES.
Properties are set to a table defined by a TYPE and ID is a unique identifier in the entire graph.
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
    /// Preferably the vertices in the data file are sorted in an ascending order by their ids.
    /// </summary>
    internal sealed class VerticesListProcessor : IProcessor<List<Vertex>>
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
        /// First state of the processor.
        /// Tries to parse ID of a node and inits a new vertex.
        /// </summary>
        sealed class NodeIDState : IProcessorState<List<Vertex>>
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
                proc.vertex.ID = id;

                // Next state is a parsing of a TYPE
                proc.SetNewState(NodeTypeState.Instance);
            }
        }

        /// <summary>
        /// Class provides a method for finishing reading of parameters of the vertex.
        /// If reading of parameters of the vertex was finished then the next state is parsing of the ID, that is reading a new vertex.
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
                    // Add the new vertex to the list of vertices.
                    proc.vertices.Add(proc.vertex);
                    // Try to read another vertex.
                    proc.SetNewState(NodeIDState.Instance);
                }
                // Else continue in parsing parameters
                else proc.SetNewState(NodeParametersState.Instance);
            }
        }


        /// <summary>
        /// Finds a table of the vertex based on a given parameter and sets it to the vertex.
        /// Also, inserts ID of the vertex into the table.
        /// Next state parses data of the vertex.
        /// </summary>
        sealed class NodeTypeState : NodeParamsEndState
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
                proc.vertex.Table = table;
                proc.vertex.Table.AddID(proc.vertex.ID);

                // Start reading properties of the vertex
                proc.paramsToReadLeft = proc.vertex.Table.GetPropertyCount();
                FinishParams(processor);
            }
        }

        /// <summary>
        /// The property of the vertex is expected.
        /// Get the position of the property where adding the passed parameter.
        /// Add the parameter there and try to read another property.
        /// </summary>
        sealed class NodeParametersState : NodeParamsEndState
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

                // Get the position of a property inside the table of the out edge. 
                int accessedPropertyPosition = proc.vertex.Table.GetPropertyCount() - proc.paramsToReadLeft;

                // Parse the value from parameter.
                proc.vertex.Table.Properties[accessedPropertyPosition].ParsePropFromStringToList(param);

                // Try to read another property.
                proc.paramsToReadLeft--;
                FinishParams(proc);
            }
        }
    }
}
