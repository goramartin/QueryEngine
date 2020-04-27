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
    class QueryOrderBy
    {
        private List<IRowProxyComparer> comparers;

        /// <summary>
        /// Creates Order by object. 
        /// </summary>
        /// <param name="comparers"> List of comparers that the results will be sorted with.</param>
        private QueryOrderBy(List<IRowProxyComparer> comparers)
        {
            this.comparers = comparers;
        }

        /// <summary>
        /// Tries to create an order by object if it failes to parse it, it returns null.
        /// </summary>
        /// <param name="tokens"> Tokens to parse.</param>
        /// <param name="graph"> Graph the query is computed on. </param>
        /// <param name="variableMap"> Map of query variables. </param>
        /// <returns> Null if there is no order by token or QueryOrderBy object.</returns>
        public static QueryOrderBy CreateOrderBy(List<Token> tokens, Graph graph, VariableMap variableMap)
        {
            OrderByNode orderNode = Parser.ParseOrderBy(tokens);
            if (orderNode == null) return null;
            else
            {
                var orderVisitor = new OrderByVisitor(graph.Labels, variableMap);
                orderVisitor.Visit(orderNode);
                var comparers = orderVisitor.GetResult();
                return new QueryOrderBy(comparers);
            }
        }
    }
}
