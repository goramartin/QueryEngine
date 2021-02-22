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


// Comment to omit printing of the results.
//#define NO_PRINT


using System;
using System.Collections.Generic;

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
        private List<ExpressionToStringWrapper> rowFormat;
        private ISelectExecutionHelper helper;
        public bool allowPrint;
        
        /// <summary>
        /// Creates Select object.
        /// Parsing is done beforehand because first we need to parse match expression for variable definitions.
        /// </summary>
        /// <param name="graph"> Property graph. </param>
        /// <param name="map"> Variable map. </param>
        /// <param name="executionHelper"> Select execution helper. </param>
        /// <param name="selectNode"> Parsed tree of select expression. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public SelectObject(Graph graph, VariableMap map, ISelectExecutionHelper executionHelper, SelectNode selectNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || selectNode == null || exprInfo == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to constructor. ");

            this.helper = executionHelper;
            
            // Process parse tree and create list of variables to be printed
            SelectVisitor visitor = new SelectVisitor(graph.labels, map, exprInfo);
            selectNode.Accept(visitor);
            this.rowFormat = visitor.GetResult();
        }

        public override void Compute(out ITableResults resTable, out GroupByResults groupByResults)
        {
            if (next != null)
            {
                this.next.Compute(out resTable, out groupByResults);
                this.next = null;

                if (allowPrint)
                    this.Print(resTable, groupByResults);
            }
            else throw new NullReferenceException($"{this.GetType()}, next is set to null."); 
        }

        private void Print(ITableResults resTable, GroupByResults groupByResults)
        {
            var printer = Printer.Factory(this.helper.Printer, rowFormat, this.helper.Formater, this.helper.FileName);
            printer.PrintHeader();
            
            if (!this.helper.IsSetGroupBy && !this.helper.IsSetSingleGroupGroupBy) Print(resTable, printer);
            else Print(groupByResults, printer);
            
            printer.Dispose();
        }

        private void Print(ITableResults resTable, Printer printer)
        {
            if (resTable == null) throw new ArgumentNullException($"{this.GetType()}, recieved table results as null.");
            else
            {
                foreach (var item in resTable)
                {
                    printer.PrintRow(item);
                }
            }
        }

        private void Print(GroupByResults results, Printer printer)
        {
            if (results == null) throw new ArgumentNullException($"{this.GetType()}, recieved group results as null.");
            else
            {
                if (results.GetType() == typeof(GroupByResultsArray))
                {
                    var tmpResults = (GroupByResultsArray)results;
                    foreach (var item in tmpResults)
                        printer.PrintRow(item);
                }
                else if (results is GroupByResultsBucket)
                {
                    var tmpResults = (GroupByResultsBucket)results;
                    foreach (var item in tmpResults)
                        printer.PrintRow(item);
                }
                else if (results is GroupByResultsList)
                {
                    var tmpResults = (GroupByResultsList)results;
                    foreach (var item in tmpResults)
                        printer.PrintRow(item);
                } else if (results is GroupByResultsStreamedBucket)
                {
                    var tmpResults = (GroupByResultsStreamedBucket)results;
                    foreach (var item in tmpResults)
                        printer.PrintRow(item);
                }
                else throw new ArgumentException($"{this.GetType()}, received unknown group result holder. holder = {results.GetType()}.");
               
            }
        }
    }



}
