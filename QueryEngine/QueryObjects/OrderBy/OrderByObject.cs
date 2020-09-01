/*! \file 
 
    This file includes definition of a order by object.
    His purpose is to contain information about sorting of results from a query.
    It contains a list of comparers that will be used during sorting.
    
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class represents order by part of a query.
    /// It sorts given results with defined comparers.
    /// </summary>
    internal sealed class OrderByObject
    {
        private List<ResultRowComparer> comparers;

        /// <summary>
        /// Creates Order by object. 
        /// </summary>
        /// <param name="comparers"> List of comparers that the results will be sorted with.</param>
        private OrderByObject(List<ResultRowComparer> comparers)
        {
            this.comparers = comparers;
        }

        /// <summary>
        /// Tries to create an order by object if it failes to parse it, it returns null.
        /// </summary>
        /// <param name="tokens"> Tokens to parse.</param>
        /// <param name="graph"> Graph the query is computed on. </param>
        /// <param name="variableMap"> Map of query variables. </param>
        /// <param name="executionHelper"> Orderby execution helper. </param>
        /// <returns> Null if there is no order by token or QueryOrderBy object.</returns>
        public static OrderByObject CreateOrderBy(List<Token> tokens, Graph graph, VariableMap variableMap, OrderByExecutionHelper executionHelper)
        {
            OrderByNode orderNode = Parser.ParseOrderBy(tokens);
            if (orderNode == null)
            {
                executionHelper.IsSetOrderBy = false;
                return null;
            }
            else
            {
                var orderVisitor = new OrderByVisitor(graph.Labels, variableMap);
                orderVisitor.Visit(orderNode);
                var comparers = orderVisitor.GetResult();

                executionHelper.IsSetOrderBy = true;
                return new OrderByObject(comparers);
            }
        }

        /// <summary>
        /// Sorts given data.
        /// </summary>
        /// <param name="sortData"> Query reults to be sorted. </param>
        /// <param name="executionHelper"> Order by execution helper. </param>
        /// <returns> Sorted data. </returns>
        public ITableResults Sort(ITableResults sortData, OrderByExecutionHelper executionHelper)
        {
             Sorter sorter = new MultiColumnSorter(sortData, this.comparers, executionHelper.IsParallel());
             var sortedResults =  sorter.Sort();

            TimeSpan ts = QueryEngine.stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Sort time " + elapsedTime);
            
            
            return sortedResults;
        }
    }
}
