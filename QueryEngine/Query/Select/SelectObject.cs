/*! \file
File contains definition of select object.
The class consists of a list of expressions enclosed in a generic wrapper.
The wrapper helps to compute expression values and convert them to string.

The list it self can be perceived as one row of the output.
That is to say, for each individual result, the expressions are computed and printed on
the same line.

The printing is done via printer class that contains a formater class.
The printer class defines where the output will be printed and the formater 
formats the output into desired format.

The results are always printed in a form of a table. A header of the table is created
by calling ToString() method on the list of expressions.
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
    internal sealed class SelectObject : QueryObject
    {
        /// <summary>
        /// List of arguments to print from a select expression.
        /// </summary>
        private readonly List<ExpressionToStringWrapper> rowFormat;
        private ISelectExecutionHelper helper;
        /// <summary>
        /// Creates Select object.
        /// Parsing is done beforehand because first we need to parse match expression for variable definitions.
        /// </summary>
        /// <param name="graph"> Property graph. </param>
        /// <param name="map"> Variable map. </param>
        /// <param name="executionHelper"> Select execution helper. </param>
        /// <param name="selectNode"> Parsed tree of select expression. </param>
        public SelectObject(Graph graph, VariableMap map, ISelectExecutionHelper executionHelper, SelectNode selectNode)
        {
            if (executionHelper == null || selectNode == null || executionHelper.Printer == null || executionHelper.Formater == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to constructor. ");

            this.helper = executionHelper;
            
                // Process parse tree and create list of variables to be printed
                SelectVisitor visitor = new SelectVisitor(graph.labels, map);
                selectNode.Accept(visitor);
                this.rowFormat = visitor.GetResult();
        }

        public override void Compute(out ITableResults results)
        {
            if (next != null)
            {
                this.next.Compute(out results);
                this.next = null;
                this.Print(results);
            }
            else throw new NullReferenceException($"{this.GetType()}, next is set to null."); 
        }

        /// <summary>
        /// Prints results in given format from concstructor init.
        /// </summary>
        /// <param name="results"> Results from query. </param>
        private void Print(ITableResults results)
        {
            var printer = Printer.Factory(this.helper.Printer, rowFormat, this.helper.Formater, this.helper.FileName);

            printer.PrintHeader();
            foreach (var item in results)
                printer.PrintRow(item);

            printer.Dispose();
        }
    }



}
