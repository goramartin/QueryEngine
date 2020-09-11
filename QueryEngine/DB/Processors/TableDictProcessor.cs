/*! \file 
File includes definition of a table dictionary processor.

The processor creates a table from a JSON input file.
Json array is expected to be an array of objects. 
The objects are expected to contain a first property "Kind" with defines name of the table.
Subsequently there are expected to be a properties that define name of a table property and the type of the property.

Example:

[
{
"Kind": "TableOne",
"FirstProp": "string"
},
{
"Kind: "TableTwo"
}
]


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
    /// Creates a distionary/map of a data types in the graph from a json schema.
    /// The processing is done in states, where each state represents a string from the json schema. (even the [, { ... characters)
    /// Firstly, the new table is created with the name specified in the "Kind" property in the JSON schema.
    /// Subsequnetly, properties are parsed and added to the table.
    /// </summary>
    internal sealed class TableDictProcessor : IProcessor<Dictionary<string, Table>>
    {
        private IProcessorState<Dictionary<string, Table>> processorState;
        private IProcessorState<Dictionary<string, Table>> lastProcessorState;

        private Dictionary<string, Table> dict;
        private Table newTable;
        private string newPropName;
        private bool finished;

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


        /// <summary>
        /// Begining of the JSON array which contains the definitions of the tables.
        /// </summary>
        sealed class TableDictLeftSquareBraceState : IProcessorState<Dictionary<string, Table>>
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

        /// <summary>
        /// Start of the table object in the JSON schama.
        /// </summary>
        sealed class TableDictLeftBracketState : IProcessorState<Dictionary<string, Table>>
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

        /// <summary>
        /// Reading of the starting """ can lead further into more states, the states are defined based on the preceeding state.
        /// </summary>
        sealed class TableDictLeftMarkState : IProcessorState<Dictionary<string, Table>>
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

                // The next string is "Kind": "Name"
                if (proc.lastProcessorState == TableDictLeftBracketState.Instance) proc.SetNewState(TableDictKindState.Instance);
                // The next string is a name of the table. 
                else if (proc.lastProcessorState == TableDictKindState.Instance) proc.SetNewState(TableDictNameState.Instance);
                // The next string is a name of a property "PropName": "PropType"
                else if (proc.lastProcessorState == TableDictNameState.Instance) proc.SetNewState(TableDictPropNameState.Instance);
                // The next string is a type of the property 
                else if (proc.lastProcessorState == TableDictPropNameState.Instance) proc.SetNewState(TableDictPropTypeState.Instance);
                // The next string is a name of a property
                else if (proc.lastProcessorState == TableDictPropTypeState.Instance) proc.SetNewState(TableDictPropNameState.Instance);
                else throw new ArgumentException(($"{this.GetType()}, unexpected state occured."));
            }
        }

        /// <summary>
        /// Parsing of the "Kind"
        /// </summary>
        sealed class TableDictKindState : IProcessorState<Dictionary<string, Table>>
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

        /// <summary>
        /// Parsing of a ending """
        /// </summary>
        sealed class TableDictRightMarkState : IProcessorState<Dictionary<string, Table>>
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

        /// <summary>
        /// Parsing of the ":"
        /// </summary>
        sealed class TableDictDoubleDotState : IProcessorState<Dictionary<string, Table>>
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
        /// Processes name of the table. 
        /// Creates a new table with the given name.
        /// </summary>
        sealed class TableDictNameState : IProcessorState<Dictionary<string, Table>>
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

        /// <summary>
        /// After a property value, there can be either a comma which signals another property definition,
        /// or a bracket, which signal that there might be another comma == a new table object or end of the json array..
        /// </summary>
        sealed class TableDictCommaAfterPropState : IProcessorState<Dictionary<string, Table>>
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

        /// <summary>
        /// After a bracket, there can be a comma which can signal beginning of a new table object.
        /// Or there is end of the json array (end of a schema).
        /// </summary>
        sealed class TableDictCommaAfterBracketState : IProcessorState<Dictionary<string, Table>>
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
        /// Saves a property name for a property class creation to the table being created later on.
        /// </summary>
        sealed class TableDictPropNameState : IProcessorState<Dictionary<string, Table>>
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
        /// Creates a new proprty based on type with a property name stored beforehand.
        /// </summary>
        sealed class TableDictPropTypeState : IProcessorState<Dictionary<string, Table>>
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
