using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class OrderByVisitor : IVisitor<List<ExpressionComparer>>
    {
        private List<ExpressionComparer> result;
        private Dictionary<string, Tuple<int, Type>> labels;
        private VariableMap variableMap;
        private ExpressionHolder expressionHolder;
        private QueryExpressionInfo exprInfo;

        public OrderByVisitor(Dictionary<string, Tuple<int, Type>> labels, VariableMap map, QueryExpressionInfo exprInfo)
        {
            this.result = new List<ExpressionComparer>();
            this.labels = labels;
            this.variableMap = map;
            this.exprInfo = exprInfo;
        }

        public List<ExpressionComparer> GetResult()
        {
            if (this.result == null || this.result.Count == 0)
                throw new ArgumentException($"{this.GetType()} final result is empty or null");
            return this.result;
        }

        /// <summary>
        /// The root of the parse tree.
        /// Jumps to the node under the root.
        /// </summary>
        public void Visit(OrderByNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException($"{ this.GetType()}, failed to parse select expr.");
        }

        /// <summary>
        /// Expects expression and possibly next order term node.
        /// </summary>
        public void Visit(OrderTermNode node)
        {
            if (node.exp == null) throw new ArgumentNullException($"{this.GetType()}, failed access expression.");
            else node.exp.Accept(this);

            this.result.Add(ExpressionComparer.Factory(this.expressionHolder, node.isAscending,
                                                      this.expressionHolder.ExpressionType));

            if (node.next != null) node.next.Accept(this);
        }

        /// <summary>
        /// Expects "Expression as label"
        /// Parses expression nodes and tries to get a label for the expression.
        /// At the end, it creates a expression holder.
        /// </summary>
        public void Visit(ExpressionNode node)
        {
            string label = null;
            ExpressionBase expr = null;

            // Parse expression.
            if (node.exp == null)
                throw new ArgumentException($"{this.GetType()}, expected expression.");
            else
            {
                var tmpVisitor = new ExpressionVisitor(this.labels, this.variableMap, this.exprInfo);
                node.exp.Accept(tmpVisitor);
                expr = tmpVisitor.GetResult();
            }

            // Try get a label for entire expression.
            if (node.asLabel != null)
                label = ((IdentifierNode)(node.asLabel)).value;

            this.expressionHolder = new ExpressionHolder(expr, label);
            this.expressionHolder = this.exprInfo.Exprs[this.exprInfo.AddExpression(this.expressionHolder)];
        }

        #region NotImpl
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchDividerNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(InEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(OutEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AnyEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchVariableNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(SelectPrintTermNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(GroupByNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(GroupByTermNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AggregateFuncNode node)
        {
            throw new NotImplementedException();
        }

        #endregion NotImpl

    }

}
