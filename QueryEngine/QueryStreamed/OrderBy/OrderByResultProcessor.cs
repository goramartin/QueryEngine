using System;

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

        protected OrderByResultProcessor(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount)
        {
            if (executionHelper == null || orderByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            this.executionHelper = executionHelper;

            var orderByVisitor = new OrderByVisitor(graph.labels, variableMap, exprInfo);
            orderByVisitor.Visit(orderByNode);
            var comps = orderByVisitor.GetResult();

            executionHelper.IsSetOrderBy = true;
            this.comparers = comps.ToArray();
            this.ColumnCount = columnCount;
        }

        protected OrderByResultProcessor(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount)
        {
            this.comparers = comparers;
            this.executionHelper = executionHelper;
            this.ColumnCount = columnCount;
        }

        private static ExpressionComparer[] ParseOrderBy(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount)
        {
            if (executionHelper == null || orderByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"Order by result processor, passing null arguments to the constructor.");

            var orderByVisitor = new OrderByVisitor(graph.labels, variableMap, exprInfo);
            orderByVisitor.Visit(orderByNode);
            var comps = orderByVisitor.GetResult();
            executionHelper.IsSetOrderBy = true;

            return comps.ToArray();
        }

        /// <summary>
        /// Constructs Order by result processor.
        /// The suffix HS stands for Half Streamed solution, whereas the S stands for Full Streamed solution.
        /// </summary>
        public static ResultProcessor Factory(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount)
        {
            if (executionHelper.SorterAlias == "abtreeHS") return new ABTreeHalfStreamedSorter(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount);
            else if (executionHelper.SorterAlias == "abtreeS")
            {
                var tmpComps = ParseOrderBy(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount);
                var typeOfFirstKey = exprInfo.OrderByComparerExprs[0].GetExpressionType();
                if (typeOfFirstKey == typeof(int)) return new ABTreeStreamedSorter<int>(tmpComps, executionHelper, columnCount);
                else if (typeOfFirstKey == typeof(string)) return new ABTreeStreamedSorter<string>(tmpComps, executionHelper, columnCount);
                else throw new ArgumentException($"Order by result processor factory, trying to create an unknown type of the streamed sorted.");
            }
            else throw new ArgumentException($"Order by result processor factory, trying to create an unknown sorter.");
        }
    }
}
