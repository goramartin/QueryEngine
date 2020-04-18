/*! \file 
  File includes definition of table dictionary processor.

  Processor gets input strings and expects to be the given strings from a json array.
  Processor creates a dictionaty of tables defined inside the json array.

  Json array is expected to have object containing first property as a Kind with defines name of the table.
  Subsequently there are expected to be a properties that define name of a table property and the type of the property.

  States are singletons and flyweight since they do not encompass eny additional variables.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Creates distionary/map from data scheme with specific nodes in the graph.
    /// </summary>
    class TableDictProcessor : IProcessor<Dictionary<string, Table>>
    {
        IProcessorState<Dictionary<string, Table>> processorState;
        IProcessorState<Dictionary<string, Table>> lastProcessorState;

        Dictionary<string, Table> dict;
        Table newTable;
        string newPropName;
        bool finished;
        public TableDictProcessor()
        {
            this.dict = new Dictionary<string, Table>();
            this.finished = false;
            this.processorState = TableDictLeftSquareBraceState.Instance;
            this.newTable = null;
            this.newPropName = null;
        }

        public bool Finished()
        {
            return this.finished;
        }

        public Dictionary<string, Table> GetResult()
        {
            return this.dict;
        }

        /// <summary>
        /// A jump table which defines what method will be called in a given state.
        /// </summary>
        /// <param name="param"> Parameter to process. </param>
        public void Process(string param)
        {
           if (!this.finished) this.processorState.Process(this, param);
        }

        public void SetNewState(IProcessorState<Dictionary<string, Table>> state)
        {
            this.processorState = state;
        }

        public void PassParameters(params object[] prms)
        {
            this.dict = (Dictionary<string, Table>)prms[0];
        }


        class TableDictLeftSquareBraceState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictLeftSquareBraceState instance =
             new TableDictLeftSquareBraceState();

            private TableDictLeftSquareBraceState() { }

            public static TableDictLeftSquareBraceState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param != "[") throw new ArgumentException($"{this.GetType()}, failed to parse types of table, expected [.");
                proc.SetNewState(TableDictLeftBracketState.Instance);
            }
        }

        class TableDictLeftBracketState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictLeftBracketState instance =
             new TableDictLeftBracketState();

            private TableDictLeftBracketState() { }

            public static TableDictLeftBracketState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param != "{") throw new ArgumentException($"{this.GetType()}, expected left Bracket");
                proc.lastProcessorState = TableDictLeftBracketState.Instance;
                proc.SetNewState(TableDictLeftMarkState.Instance);
            }
        }

        class TableDictLeftMarkState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictLeftMarkState instance =
             new TableDictLeftMarkState();

            private TableDictLeftMarkState() { }

            public static TableDictLeftMarkState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param != "\"") throw new ArgumentException(($"{this.GetType()}, expected left quotations."));

                if (proc.lastProcessorState == TableDictLeftBracketState.Instance) proc.SetNewState(TableDictKindState.Instance);
                else if (proc.lastProcessorState == TableDictKindState.Instance) proc.SetNewState(TableDictNameState.Instance);
                else if (proc.lastProcessorState == TableDictNameState.Instance) proc.SetNewState(TableDictPropNameState.Instance);
                else if (proc.lastProcessorState == TableDictPropNameState.Instance) proc.SetNewState(TableDictPropTypeState.Instance);
                else if (proc.lastProcessorState == TableDictPropTypeState.Instance) proc.SetNewState(TableDictPropNameState.Instance);
            }
        }

        class TableDictKindState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictKindState instance =
             new TableDictKindState();

            private TableDictKindState() { }

            public static TableDictKindState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param != "Kind")
                    throw new ArgumentException($"{this.GetType()}, expected Kind");
                proc.lastProcessorState = TableDictKindState.Instance;
                proc.SetNewState(TableDictRightMarkState.Instance);
            }
        }

        class TableDictRightMarkState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictRightMarkState instance =
             new TableDictRightMarkState();

            private TableDictRightMarkState() { }

            public static TableDictRightMarkState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param != "\"")
                    throw new ArgumentException($"{this.GetType()}, expected \"");
                if ((proc.lastProcessorState == TableDictKindState.Instance) || 
                    (proc.lastProcessorState == TableDictPropNameState.Instance))
                    proc.SetNewState(TableDictDoubleDotState.Instance);
                else proc.SetNewState(TableDictCommaAfterPropState.Instance);
            }
        }

        class TableDictDoubleDotState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictDoubleDotState instance =
             new TableDictDoubleDotState();

            private TableDictDoubleDotState() { }

            public static TableDictDoubleDotState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param != ":") throw new ArgumentException($"{this.GetType()}, expected :");
                proc.SetNewState(TableDictLeftMarkState.Instance);
            }
        }

        /// <summary>
        /// Processes name of the table. Call for creating of a table.
        /// </summary>
        class TableDictNameState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictNameState instance =
             new TableDictNameState();

            private TableDictNameState() { }

            public static TableDictNameState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                proc.newTable = new Table(param);
                if (proc.dict.ContainsKey(param))
                    throw new ArgumentException($"{this.GetType()}, adding table that exists. Table = {param}");
                else proc.dict.Add(param, proc.newTable);
                proc.lastProcessorState = TableDictNameState.Instance;
                proc.SetNewState(TableDictRightMarkState.Instance);
            }
        }

        class TableDictCommaAfterPropState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictCommaAfterPropState instance =
             new TableDictCommaAfterPropState();

            private TableDictCommaAfterPropState() { }

            public static TableDictCommaAfterPropState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param == ",") proc.SetNewState(TableDictLeftMarkState.Instance);
                else if (param == "}") proc.SetNewState(TableDictCommaAfterBracketState.Instance);
            }
        }

        class TableDictCommaAfterBracketState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictCommaAfterBracketState instance =
             new TableDictCommaAfterBracketState();

            private TableDictCommaAfterBracketState() { }

            public static TableDictCommaAfterBracketState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                if (param == ",") proc.SetNewState(TableDictLeftBracketState.Instance);
                else if (param == "]")
                {
                    proc.finished = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Saves property name for a later usage.
        /// </summary>
        class TableDictPropNameState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictPropNameState instance =
             new TableDictPropNameState();

            private TableDictPropNameState() { }

            public static TableDictPropNameState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                proc.newPropName = param;
                proc.lastProcessorState = TableDictPropNameState.Instance;
                proc.SetNewState(TableDictRightMarkState.Instance);
            }
        }

        /// <summary>
        /// Processes property type.
        /// Creates new proprty based on type with a property name stored before.
        /// </summary>
        class TableDictPropTypeState : IProcessorState<Dictionary<string, Table>>
        {
            static TableDictPropTypeState instance =
             new TableDictPropTypeState();

            private TableDictPropTypeState() { }

            public static TableDictPropTypeState Instance
            {
                get { return instance; }
            }

            public void Process(IProcessor<Dictionary<string, Table>> processor, string param)
            {
                var proc = (TableDictProcessor)processor;

                Property newProp = PropertyFactory.CreateProperty(param, proc.newPropName);
                proc.newTable.AddNewProperty(newProp);


                proc.lastProcessorState = TableDictPropTypeState.Instance;
                proc.SetNewState(TableDictCommaAfterPropState.Instance);
            }
        }
    }
}
