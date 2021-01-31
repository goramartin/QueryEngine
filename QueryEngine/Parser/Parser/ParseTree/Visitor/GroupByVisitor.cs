using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class GroupByVisitor : IVisitor<List<ExpressionHolder>>
    {
        private List<ExpressionHolder> holders;
        private Dictionary<string, Tuple<int, Type>> labels;
        private VariableMap variableMap;
        private QueryExpressionInfo exprInfo;

        public GroupByVisitor(Dictionary<string, Tuple<int, Type>> labels, VariableMap map, QueryExpressionInfo exprInfo)
        {
            this.holders = new List<ExpressionHolder>();
            this.labels = labels;
            this.variableMap = map;
            this.exprInfo = exprInfo;
        }

        public List<ExpressionHolder> GetResult()
        {
            if (holders == null || holders.Count == 0)
                throw new ArgumentException($"{this.GetType()}, the final results is empty or null.");
            else return this.holders;
        }
        
        /// <summary>
        /// A root node of the parse tree.
        /// Jumps to thxe node under the root.
        /// There must be always at least one result.
        /// </summary>
        public void Visit(GroupByNode node)
        {
            node.next.Accept(this);
            if (this.holders.Count == 0 )
                throw new ArgumentException($"{this.GetType()}, the final results is empty or null.");
        }

        /// <summary>
        /// Expects expression and possibly the next group by term.
        /// </summary>
        public void Visit(GroupByTermNode node)
        {
            if (node.exp == null) throw new ArgumentNullException($"{this.GetType()}, failed to access expression.");
            else node.exp.Accept(this);

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
                var tmpVisitor = new ExpressionVisitor(this.labels, this.variableMap, this.exprInfo);
                node.exp.Accept(tmpVisitor);
                expr = tmpVisitor.GetResult();
            }

            // Try get a label for entire expression.
            if (node.asLabel != null)
                label = ((IdentifierNode)(node.asLabel)).value;

            var tmpExpr = new ExpressionHolder(expr, label);
            this.holders.Add(tmpExpr);
            // The returned position always points to the passed expression.
            this.exprInfo.AddGroupByHash(tmpExpr);
        }

        #region NotImplemented
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(SelectPrintTermNode node)
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

        public void Visit(MatchVariableNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(OrderByNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(OrderTermNode node)
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

        public void Visit(AggregateFuncNode node)
        {
            throw new NotImplementedException();
        }
        #endregion NotImplemented
    }
}
