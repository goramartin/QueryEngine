using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Select represents list of variables to be printed.
    /// List of select variables contains names and proprty names to be printed from the result.
    /// </summary>
    class SelectObject
    {
        private List<SelectVariable> selectVariables;
        string FormaterType { get; }
        string PrinterType { get; }
        string FileName { get; }


        /// <summary>
        /// Creates Select expression
        /// </summary>
        /// <param name="tokens"> Tokens to be parsed. (Expecting first token to be a Select token.)</param>
        /// <param name="printer"> Type of printer. </param>
        /// <param name="formater"> Type of formater. </param>
        /// <param name="fileName"> File name where to print results if chosen file printer. </param>
        public SelectObject(List<Token> tokens, string printer, string formater, string fileName = null)
        {
            if (printer == null || formater == null) throw new ArgumentNullException($"{this.GetType()}, got printer or formater as null.");
            else { this.FormaterType = formater; this.FileName = fileName; this.PrinterType = printer; }

            // Create tree of select part of query
            SelectNode selectNode = Parser.ParseSelectExpr(tokens);

            // Process parse tree and create list of variables to be printed
            SelectVisitor visitor = new SelectVisitor();
            selectNode.Accept(visitor);

            this.selectVariables = visitor.GetResult();
        }

        public List<SelectVariable> GetSelectVariables() => this.selectVariables;


        /// <summary>
        /// Checks correctness of given print expression.
        /// There must be either * or variable references with their properties.
        /// </summary>
        /// <param name="variableMap"> Map of variables. </param>
        public void CheckCorrectnessOfSelect(VariableMap variableMap)
        {
            if (this.selectVariables[0].name == "*")
            {
                if (this.selectVariables.Count > 1)
                    throw new ArgumentException($"{this.GetType()}, select expression cannot have another variable while habing *");
            }
            else
            {
                for (int i = 0; i < this.selectVariables.Count; i++) 
                {
                    if (variableMap.TryGetValue(this.selectVariables[i].name, out Tuple<int, Table> tuple))
                    {
                        if (tuple.Item2 !=null && this.selectVariables[i].propName !=null && !tuple.Item2.ContainsProperty(this.selectVariables[i].propName))
                                throw new ArgumentException($"{this.GetType()}, select expression contains variable that is not defined");
                        
                    }
                    else throw new ArgumentException($"{this.GetType()}, select expression contains variable that is not defined");
                }
            }
        }


        /// <summary>
        /// Prints results in given format from concstructor init.
        /// Creates structure that printer uses.
        /// </summary>
        /// <param name="results"> Results from query. </param>
        /// <param name="map"> Map of variable for creation of print variables. </param>
        public void Print(IResultStorage results, VariableMap map)
        {
            var printVars = this.CreatePrintVariables(map) ;

            var printer = Printer.PrinterFactory(this.PrinterType, printVars, this.FormaterType, this.FileName);

            printer.PrintHeader();
            foreach (var item in results)
            {
                printer.PrintRow(item);
            }

            printer.Dispose();
        }

        /// <summary>
        /// Creates list of structs that printer uses to print out headers and columns.
        /// For every variable for printing that did not stated property, for each of its property
        /// one struct will be created representing each property. The same for * but for every variable.
        /// Otherwise only one printer variable is created.
        /// </summary>
        /// <param name="map"> Map of variables. </param>
        /// <returns> List of print variables. </returns>
        private List<PrinterVariable> CreatePrintVariables(VariableMap map)
        {
            List<PrinterVariable> printVars = null;
            if (this.selectVariables[0].name == "*") return CreatePrintvariablesAsterix(map);

            printVars = new List<PrinterVariable>();
            for (int i = 0; i < this.selectVariables.Count; i++)
            {
                if (this.selectVariables[i].propName == "id") this.selectVariables[i].propName = null;
                 
                printVars.Add(PrinterVariable.PrinterVariableFactory(this.selectVariables[i],map));
            }

            return printVars;
        }

        /// <summary>
        /// Creates printer variable for every variable defined in match query.
        /// Select variables are also created with the names from variable map.
        /// This leads to printing id and a type of the variable into one column.
        /// </summary>
        /// <param name="map"> Map of variables.</param>
        /// <returns> List of print variables. </returns>
        private List<PrinterVariable> CreatePrintvariablesAsterix(VariableMap map)
        {
            var printVars = new List<PrinterVariable>();
            foreach (var item in map)
            {
                    var tmp = new SelectVariable();
                    tmp.TrySetName(item.Key);
                    printVars.Add(PrinterVariable.PrinterVariableFactory(tmp, map));
            }
            return printVars;
        }        


    }


    /// <summary>
    /// Class is used as a holder for printing directives.
    /// When select initialises printing the class will serve as a base information for printing variables.
    /// </summary>
    class SelectVariable
    {
        public string name { get; set; }
        public string propName { get; set; }
        public string label { get; set; }

        public bool TrySetName(string n)
        {
            if (this.name == null) { this.name = n; return true; }
            else return false;
        }
        public bool TrySetPropName(string n)
        {
            if (this.propName == null) { this.propName = n; return true; }
            else return false;
        }

        public bool TrySetLabel(string l)
        {
            if (this.label == null) { this.label = l; return true; }
            else return false;
        }

        public bool IsEmpty()
        {
            if ((this.name == null) && (this.propName == null) && (this.label == null)) return true;
            else return false;
        }
    }


}
