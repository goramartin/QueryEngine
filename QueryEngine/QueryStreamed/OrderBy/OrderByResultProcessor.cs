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
        protected int[] usedVars;

        protected OrderByResultProcessor(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars)
        {
            this.comparers = comparers;
            this.executionHelper = executionHelper;
            this.ColumnCount = columnCount;
            this.usedVars = usedVars;
        }

        public static ExpressionComparer[] ParseOrderBy(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount)
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
        public static ResultProcessor Factory(QueryExpressionInfo exprInfo, ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars)
        {
            if (executionHelper.SorterAlias == "abtreeHS") return new ABTreeHalfStreamedSorter(comparers, executionHelper, columnCount, usedVars);
            else if (executionHelper.SorterAlias == "abtreeS")
            {
                var typeOfFirstKey = exprInfo.OrderByComparerExprs[0].GetExpressionType();
                if (typeOfFirstKey == typeof(int)) return new ABTreeStreamedSorter<int>(comparers, executionHelper, columnCount, usedVars);
                else if (typeOfFirstKey == typeof(string)) return new ABTreeStreamedSorter<string>(comparers, executionHelper, columnCount, usedVars);
                else throw new ArgumentException($"Order by result processor factory, trying to create an unknown type of the streamed sorted.");
            }
            else throw new ArgumentException($"Order by result processor factory, trying to create an unknown sorter.");
        }
    }
}
