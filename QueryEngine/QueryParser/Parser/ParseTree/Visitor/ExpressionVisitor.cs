using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Visitor used to parse expressions.
    /// So far there are implemented only variable references and aggregation references.
    /// There can be only either the var or agg ref. that means that the Expr variable
    /// contains only variantions of these two classes.
    /// </summary>
    internal sealed class ExpressionVisitor : IVisitor<ExpressionBase>
    {
        private ExpressionBase Expr;
        private VariableReferenceNameHolder nameHolder;
        private VariableMap variableMap;
        private Dictionary<string, Tuple<int, Type>> labels;
        private QueryExpressionInfo exprInfo;

        public ExpressionBase GetResult()
        {
            return this.Expr;
        }

        public ExpressionVisitor(Dictionary<string, Tuple<int, Type>> labels, VariableMap map, QueryExpressionInfo exprInfo)
        {
            this.variableMap = map;
            this.labels = labels;
            this.exprInfo = exprInfo;
        }

        /// <summary>
        /// If it consists only of a name, variable id reference is created.
        /// Otherwise property reference will be created.
        /// </summary>
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
        /// Sets only name of the variable and the name of accessed property.
        /// </summary>
        public void Visit(IdentifierNode node)
        {
            if (node.value == null) throw new ArgumentNullException($"{this.GetType()}, identifier value is set to null.");
            else if (this.nameHolder.TrySetName(node.value)) return;
            else if (this.nameHolder.TrySetPropName(node.value)) return;
            else throw new ArgumentException($"{this.GetType()}, expected new name holder.");
        }

        /// <summary>
        /// Visits aggregation node.
        /// Creates a new aggregation function based on the provided name.
        /// And initilises parsing of the aggregation argument.
        /// </summary>
        public void Visit(AggregateFuncNode node)
        {
            Aggregate aggregate = null;
            Type aggType = null;

            if (node.next == null) throw new ArgumentException($"{this.GetType()}, exprected aggregation arguments.");
            else
            {
                // count(*)
                if (node.next.GetType() == typeof(IdentifierNode))
                {
                    if (node.funcName.ToLower() == "count" && ((IdentifierNode)node.next).value == "*")
                    {
                        aggregate = Aggregate.Factory("count", typeof(int), null);
                        aggType = typeof(int);
                    }
                    else throw new ArgumentException($"{this.GetType()}, expected count(*).");
                }  
                else
                {
                    // Every other aggregation
                    // The only possibility is that the next node is VariableNode.
                    // So the argument will be created in this.Expr, from this expr the holder must be created.
                    // After the holder is created the aggregation is created with the expression.
                    // After this process, the expression that will be returned is created -> aggregation reference.
                    node.next.Accept(this);
                    var tmpHolder = new ExpressionHolder(this.Expr);
                    aggregate = Aggregate.Factory(node.funcName.ToLower(), tmpHolder.ExpressionType, tmpHolder);
                    aggType = tmpHolder.ExpressionType;
                }
            }

            // Rewrite the expression used for aggregation argument to aggregation reference.
            int aggPos = this.exprInfo.AddAggregate(aggregate);
            this.Expr = AggregateReferenceFactory.Create(aggType, aggPos, this.exprInfo.aggregates[aggPos]);
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

        public void Visit(GroupByNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(GroupByTermNode node)
        {
            throw new NotImplementedException();
        }


        #endregion NotImpl
    }

}
