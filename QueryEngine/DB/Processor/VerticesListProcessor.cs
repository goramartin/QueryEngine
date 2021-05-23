/*! \file 
The file includes definition of a vertices List processor.

The processor creates a List of vertices. The edges position for each vertex are not filled (set to -1).
The processor expects the vertices to have a unique id, preferably sorted by ascending order.

The input file should look like: ID TYPE PROPERTIES.
PROPERTIES are set to a table defined by a TYPE and ID is a unique identifier in the entire graph.
Hence, the ID is not directly a property of the element.

States of a processor are singletons and flyweight since they do not encompass any additional varibales.
 */

using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Creates a vertices List from a file.
    /// Preferably the vertices in the data file are sorted in an ascending order by their ids.
    /// </summary>
    internal sealed class VerticesListProcessor : IProcessor<List<Vertex>>
    {
        private IProcessorState<List<Vertex>> processorState;
        private List<Vertex> vertices;
        private Dictionary<string, Table> nodeTables;
        private Vertex vertex;
        private int paramsToReadLeft;
        private bool finished;

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
        /// The first state of the processor.
        /// Tries to parse an ID of a vertex and inits a new vertex.
        /// </summary>
        sealed class NodeIDState : IProcessorState<List<Vertex>>
        {
            static NodeIDState instance =
             new NodeIDState();

            int count;
            private NodeIDState() { }

            public static NodeIDState Instance
            {
                get  {   return instance;  }
            }

            public void Process(IProcessor<List<Vertex>> processor, string param)
            {
                var proc = (VerticesListProcessor)processor;

                // Just a test print.
                count++;
                if (count % 200000 == 0) Console.WriteLine(count);


                if (param == null)
                {
                    proc.finished = true;
                    return;
                };

                if (!int.TryParse(param, out int id))
                    throw new ArgumentException($"{this.GetType()}, reading wrong node ID. ID is not a number. ID = {param}");

                proc.vertex = new Vertex
                {
                    PositionInList = proc.vertices.Count,
                    ID = id
                };

                // The next state is a parsing of a TYPE.
                proc.SetNewState(NodeTypeState.Instance);
            }
        }

        /// <summary>
        /// This class provides a method for finishing reading of parameters of the vertex.
        /// If reading of parameters of the vertex was finished then the next state is parsing of the ID, that is reading a new vertex.
        /// Otherwise, we continue reading next parameters.
        /// </summary>
        abstract class NodeParamsEndState : IProcessorState<List<Vertex>>
        {
            public abstract void Process(IProcessor<List<Vertex>> processor, string param);

            protected void FinishParams(IProcessor<List<Vertex>> processor)
            {
                var proc = (VerticesListProcessor)processor;

                // For no more parameters to parse left.
                if (proc.paramsToReadLeft == 0)
                {
                    // Add the new vertex to the List of vertices.
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
        /// The next state parses data of the vertex.
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

                proc.nodeTables.TryGetValue(param, out Table table);
                proc.vertex.Table = table;
                proc.vertex.Table.AddID(proc.vertex.ID);

                // Start reading properties of the vertex
                proc.paramsToReadLeft = proc.vertex.Table.PropertyCount;
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
                var tmpTable = proc.vertex.Table;

                // Get the position of a property inside the table of the out edge. 
                int accessedPropertyPosition = tmpTable.PropertyCount - proc.paramsToReadLeft;

                // Parse the value from parameter.
                tmpTable.Properties[tmpTable.PropertyLabels[accessedPropertyPosition]].ParsePropFromStringToList(param);

                // Try to read another property.
                proc.paramsToReadLeft--;
                FinishParams(proc);
            }
        }
    }
}
