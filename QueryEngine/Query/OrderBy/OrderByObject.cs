/*! \file 
This file includes definition of a order by object.
His purpose is to contain information about sorting of results from a query.
It contains a list of comparers that will be used during sorting.

Sorting is done with the help of HPC sharp library Merge sort in both parallel and single thread cases.
Merge sort is chosen because it does the least amount of comparisons. The comparisons are really expensive
because the database need to be accessed and expression must be computed in order to compare the results rows.

The ordering works as follows.
Firstly, the array of integers is created where each index represents a index to the results table (just like pointers).
The array is then passed to the sorter with comparer. Each time the comparer compares the indeces the actual rows are
compared instead. It saves a lot of time because moving rows in a table is very time consuming.
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
        private List<IRowComparer> comparers;

        /// <summary>
        /// Creates Order by object. 
        /// </summary>
        /// <param name="comparers"> List of comparers that the results will be sorted with.</param>
        private OrderByObject(List<IRowComparer> comparers)
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
        public static OrderByObject CreateOrderBy(List<Token> tokens, Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper)
        {
            int position = 0;
            OrderByNode orderNode = Parser.ParseOrderBy(ref position, tokens);
            if (orderNode == null)
            {
                executionHelper.IsSetOrderBy = false;
                return null;
            }
            else
            {
                var orderVisitor = new OrderByVisitor(graph.labels, variableMap);
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
        public ITableResults Sort(ITableResults sortData, IOrderByExecutionHelper executionHelper)
        {
             Console.WriteLine("Order start");
             Sorter sorter = new MultiColumnSorter(sortData, this.comparers, executionHelper.InParallel);
             var sortedResults =  sorter.Sort();

             TimeSpan ts = QueryEngine.stopwatch.Elapsed;
             string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
             Console.WriteLine("Sort time " + elapsedTime);

            

            return sortedResults;
        }
    }
}
