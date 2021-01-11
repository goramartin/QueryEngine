
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class SelectVisitor : IVisitor<List<ExpressionToStringWrapper>>
    {
        private List<ExpressionToStringWrapper> result;
        private Dictionary<string, Tuple<int,Type>> labels;
        private VariableMap variableMap;
        private QueryExpressionInfo exprInfo;

        public SelectVisitor(Dictionary<string, Tuple<int, Type>> labels, VariableMap map, QueryExpressionInfo exprInfo)
        {
            this.result = new List<ExpressionToStringWrapper>();
            this.labels = labels;
            this.variableMap = map;
            this.exprInfo = exprInfo;
        }

        public List<ExpressionToStringWrapper> GetResult()
        {
            if (this.result == null || this.result.Count == 0)
                throw new ArgumentException($"{this.GetType()} final result is empty or null");
            return this.result;
        }

        /// <summary>
        /// Root node of the parse tree.
        /// Jump to the next node under root.
        /// There must be at least one variable to be displyed.
        /// </summary>
        public void Visit(SelectNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException($"{ this.GetType()}, final result is empty or null.");
        }

        /// <summary>
        /// Parses print term node.
        /// Expects that there is expression node and possibly next print term node.
        /// Together it creates a chain of print expressions.
        /// </summary>
        public void Visit(SelectPrintTermNode node)
        {
            if (node.exp == null)
                throw new ArgumentNullException($"{this.GetType()}, failed to access expression.");
            else node.exp.Accept(this);

            if (node.next != null) node.next.Accept(this);
        }

        /// <summary>
        /// Expects "Expression as label"
        /// Parses expression nodes and tries to get a label for the expression.
        /// At the end, it creates a expression holder.
        /// It returns from here because there is no other node to visit.
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

            var tmpExprHolder = this.exprInfo.Exprs[this.exprInfo.AddExpression(new ExpressionHolder(expr, label))];
            this.result.Add(ExpressionToStringWrapper.Factory(tmpExprHolder, tmpExprHolder.ExpressionType));

        }

        /// <summary>
        /// Select * case. That means that there are as many expressions as variables in the query.
        /// For each variable, expression that consists only of reference id will be created.
        /// It returns from here because there is no other node to visit.
        /// </summary>
        public void Visit(VariableNode node)
        {
            if (node.name == null || ((IdentifierNode)node.name).value != "*")
                throw new ArgumentException($"{this.GetType()}, expected asterix.");

            foreach (var item in variableMap)
            {
                var tmpExprHolder = new ExpressionHolder(new VariableIDReference(new VariableReferenceNameHolder(item.Key), item.Value.Item1), null);
                tmpExprHolder = this.exprInfo.Exprs[exprInfo.AddExpression(tmpExprHolder)];
                this.result.Add(ExpressionToStringWrapper.Factory(tmpExprHolder, tmpExprHolder.ExpressionType));
            }

        }

        #region NotImpl

        public void Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(MatchDividerNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(MatchNode node)
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
        public void Visit(AnyEdgeNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(OutEdgeNode node)
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
