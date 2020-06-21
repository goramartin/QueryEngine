
/*! \file
  
    File contains definition of select object and select variable that is parsed from a select expression.
    Select object is included inside Query class and prepresents printing of an input of a query.
    Select object remembers the columns the user wants to print and creates an appropriate printer and formater
    based on user needs.

    Notice that during creation of the select expression it only stores the expression holders.
    It is because printing needs both print variables and expression holders to specify headings. 
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
    /// List of select variables contains names and proprty names to be printed from the result.
    /// </summary>
    internal sealed class SelectObject
    {
        /// <summary>
        /// List of arguments to print from a select expression.
        /// </summary>
        private List<PrintVariable> rowFormat;
        
        /// <summary>
        /// Type of printing format.
        /// Used inside print method for factory method of formater.
        /// </summary>
        string FormaterType { get; }
        /// <summary>
        /// Type of printer for printing results.
        ///  Used inside print method for factory method of printer.
        /// </summary>
        string PrinterType { get; }
        /// <summary>
        /// File name where to print results.
        /// </summary>
        string FileName { get; }


        /// <summary>
        /// Creates Select object.
        /// Parsing is done beforehand because first we need to parse match expression for variable definitions.
        /// </summary>
        /// <param name="graph"> Property graph. </param>
        /// <param name="map"> Variable map. </param>
        /// <param name="selectNode"> Parsed tokens from input query. </param>
        /// <param name="printer"> Type of printer. </param>
        /// <param name="formater"> Type of formater. </param>
        /// <param name="fileName"> File name where to print results if chosen file printer. </param>
        public SelectObject(Graph graph, VariableMap map, SelectNode selectNode, string printer, string formater, string fileName = null)
        {
            if (printer == null || formater == null) throw new ArgumentNullException($"{this.GetType()}, got printer or formater as null.");
            else { this.FormaterType = formater; this.FileName = fileName; this.PrinterType = printer; }

            // Process parse tree and create list of variables to be printed
            SelectVisitor visitor = new SelectVisitor(graph.Labels, map);
            selectNode.Accept(visitor);

            this.rowFormat = visitor.GetResult();
        }


        /// <summary>
        /// Prints results in given format from concstructor init.
        /// </summary>
        /// <param name="results"> Results from query. </param>
        public void Print(IResults results)
        {
            var printer = Printer.PrinterFactory(this.PrinterType, rowFormat, this.FormaterType, this.FileName);

            printer.PrintHeader();
            foreach (var item in results)
            {
                printer.PrintRow(item);
            }

            printer.Dispose();
        }
    }



}
