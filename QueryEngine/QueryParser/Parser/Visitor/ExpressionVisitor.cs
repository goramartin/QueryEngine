/*! \file
  This file includes definitions of expression visitor used to collect data from created parsed tree.
  It implements visits to a classes used inside a expression parsed tree.
  Visitor creates an expression tree that is used to compute values used during query, such as values to be printed during 
  select expression or values during order by expression.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Visitor used to parse expressions.
    /// So far there are implemented only variable references as a expression.
    /// </summary>
    internal sealed class ExpressionVisitor : IVisitor<ExpressionBase>
    {
        private ExpressionBase Expr;
        private VariableReferenceNameHolder nameHolder;
        private VariableMap variableMap;
        private Dictionary<string, Tuple<int, Type>> labels;

        public ExpressionBase GetResult()
        {
            return this.Expr;
        }

        public ExpressionVisitor(VariableMap map, Dictionary<string, Tuple<int, Type>> labels)
        {
            this.variableMap = map;
            this.labels = labels;
        }

        /// <summary>
        /// Visits variable node. 
        /// If it consists only of a name, variable id reference is created.
        /// Otherwise propperty reference will be created.
        /// </summary>
        /// <param name="node"> Variable node.</param>
        public void Visit(VariableNode node)
        {
            this.nameHolder = new VariableReferenceNameHolder();

            if (node.name == null)
                throw new ArgumentException($"{this.GetType()}, expected name of a variable.");
            else
            {
                node.name.Accept(this);
                if (node.propName != null)
                    node.propName.Accept(this);
            }

            // Get the position of the variable in the result.
            int varIndex = this.variableMap.GetVariablePosition(this.nameHolder.Name);
            if (this.nameHolder.PropName == null)
                this.Expr = new VariableIDReference(this.nameHolder, varIndex);
            else
            {
                // Get type of accessed property.
                if (!this.labels.TryGetValue(this.nameHolder.PropName, out Tuple<int, Type> tuple))
                    throw new ArgumentException($"{this.GetType()}, property {this.nameHolder.PropName} does not exists in the graph.");
                else
                    this.Expr = VariableReferencePropertyFactory.Create(this.nameHolder, varIndex, tuple.Item2, tuple.Item1);
            }

        }

        /// <summary>
        /// Visits identifier node.
        /// Sets only name of variable and name of accessed property.
        /// </summary>
        /// <param name="node">Identifier node. </param>
        public void Visit(IdentifierNode node)
        {
            if (node.value == null) throw new ArgumentNullException($"{this.GetType()}, identifier value is set to null.");
            else if (this.nameHolder.TrySetName(node.value)) return;
            else if (this.nameHolder.TrySetPropName(node.value)) return;
            else throw new ArgumentException($"{this.GetType()}, expected new name holder.");
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
        public void Visit(ExpressionNode node)
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
        public void Visit(SelectPrintTermNode node)
        {
            throw new NotImplementedException();
        }

        #endregion NotImpl
    }

}
