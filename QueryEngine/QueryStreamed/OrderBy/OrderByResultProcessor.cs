using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal abstract class OrderByResultProcessor : ResultProcessor
    {
        protected ExpressionComparer[] comparers;
        protected IOrderByExecutionHelper executionHelper;
        /// <summary>
        /// Represents a number of variables defined in the match clause of the query.
        /// </summary>
        protected int ColumnCount { get; }

        protected OrderByResultProcessor(QueryExpressionInfo expressionInfo, IOrderByExecutionHelper executionHelper, int columnCount)
        {
            //this.comparers = expressionInfo.OrderByComparerExprs.ToArray();
            this.ColumnCount = columnCount;
            this.executionHelper = executionHelper;

        }

        /// <summary>
        /// Parses Order by parse tree, the information is stored in the expression info class.
        /// </summary>
        public static void ParseOrderBy(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || orderByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"Order by results processor, passing null arguments to the constructor.");

            var orderByVisitor = new OrderByVisitor(graph.labels, variableMap, exprInfo);
            orderByVisitor.Visit(orderByNode);
            executionHelper.IsSetOrderBy = true;
        }
    
        public static ResultProcessor Factory(QueryExpressionInfo expressionInfo, IOrderByExecutionHelper executionHelper, int columnCount)
        {


            return null;
        }
    }
}
