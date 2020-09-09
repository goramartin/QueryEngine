
/*! \file
File contains definition of select object and select variable that is parsed from a select expression.
Select object is included inside Query class and prepresents printing of an input of a query.
Select object remembers the columns the user wants to print and creates an appropriate printer and formater
based on the user needs.

Notice that during creation of the select expression, it only stores the expression holders.
It is because printing needs both print variables and expression holders to specify headings, but the
print variables are created only on demand when printing.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Select represents list of variables to be printed.
    /// List of select variables contains names and property names to be printed from the result.
    /// </summary>
    internal sealed class SelectObject
    {
        /// <summary>
        /// List of arguments to print from a select expression.
        /// </summary>
        private readonly List<PrintVariable> rowFormat;

        /// <summary>
        /// Creates Select object.
        /// Parsing is done beforehand because first we need to parse match expression for variable definitions.
        /// </summary>
        /// <param name="graph"> Property graph. </param>
        /// <param name="map"> Variable map. </param>
        /// <param name="selectNode"> Parsed tokens from input query. </param>
        /// <param name="executionHelper"> Select execution helper. </param>
        public SelectObject(Graph graph, VariableMap map, SelectNode selectNode, ISelectExecutionHelper executionHelper)
        {
            if (executionHelper.Printer == null || executionHelper.Formater == null) throw new ArgumentNullException($"{this.GetType()}, got printer or formater as null.");

            // For provisional Count(*);
            if (selectNode.next.GetType() == typeof(CountProvisional))
                executionHelper.IsStoringResult = false;
            else
            {
                // Process parse tree and create list of variables to be printed
                SelectVisitor visitor = new SelectVisitor(graph.Labels, map);
                selectNode.Accept(visitor);
                this.rowFormat = visitor.GetResult();
            }

        }


        /// <summary>
        /// Prints results in given format from concstructor init.
        /// </summary>
        /// <param name="results"> Results from query. </param>
        /// <param name="executionHelper"> Select execution helper. </param>
        public void Print(ITableResults results, ISelectExecutionHelper executionHelper)
        {
            // For Provisional Count(*)
            if (executionHelper.IsStoringResult == false)
            {
                Console.WriteLine("Count: {0}", results.Count);
                return; 
            }

            var printer = Printer.PrinterFactory(executionHelper.Printer, rowFormat, executionHelper.Formater, executionHelper.FileName);

            printer.PrintHeader();
            foreach (var item in results)
                printer.PrintRow(item);

            printer.Dispose();
        }
    }



}
