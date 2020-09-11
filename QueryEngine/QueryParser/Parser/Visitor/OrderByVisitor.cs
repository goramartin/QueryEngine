/*! \file
  This file includes definitions of order by visitor used to collect data from created parsed tree.
  It implements visits to a classes used inside a orderby parsed tree.
  Visitor creates a list of comparers that are used during sorting query results from match algorithm.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Creates a list or comparers that will be used during ordering of match results.
    /// </summary>
    internal sealed class OrderByVisitor : IVisitor<List<ResultRowComparer>>
    {
        private List<ResultRowComparer> result;
        private Dictionary<string, Type> Labels;
        private VariableMap variableMap;
        private ExpressionHolder expressionHolder;

        public OrderByVisitor(Dictionary<string, Type> labels, VariableMap map)
        {
            this.result = new List<ResultRowComparer>();
            this.Labels = labels;
            this.variableMap = map;
        }

        public List<ResultRowComparer> GetResult()
        {
            if (this.result == null || this.result.Count == 0)
                throw new ArgumentException($"{this.GetType()} final result is empty or null");
            return this.result;
        }

        public void Visit(OrderByNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException($"{ this.GetType()}, failed to parse select expr.");
        }

        public void Visit(OrderTermNode node)
        {
            if (node.exp == null) throw new ArgumentNullException($"{this.GetType()}, failed access expression.");
            else node.exp.Accept(this);

            this.result.Add(ExpressionComparer.
                            ExpressionCompaperFactory(this.expressionHolder, node.isAscending,
                                                      this.expressionHolder.ExpressionType));

            if (node.next != null) node.next.Accept(this);
        }
        public void Visit(ExpressionNode node)
        {
            string label = null;
            ExpressionBase expr = null;

            // Parse expression.
            if (node.exp == null)
                throw new ArgumentException($"{this.GetType()}, expected expression.");
            else
            {
                var tmpVisitor = new ExpressionVisitor(this.variableMap, this.Labels);
                node.exp.Accept(tmpVisitor);
                expr = tmpVisitor.GetResult();
            }

            // Try get a label for entire expression.
            if (node.asLabel != null)
                label = ((IdentifierNode)(node.asLabel)).value;

            this.expressionHolder = new ExpressionHolder(expr, label);
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

        #endregion NotImpl

    }

}
