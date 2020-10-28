using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class serves as a information collector of used expressions in a query.
    /// 
    /// If the group by clause is defined. In the query, there can be referenced only
    /// the same expressions as in the group clause or an aggregate functions.
    /// However, if the group by clause is not in the query but aggregates are inputed.
    /// Then, there can be only aggregates referenced. 
    /// 
    /// When collecting the aggregations and expressions, this class provides data to check
    /// against the correctness of the other expressions in the query.
    /// 
    /// Note that AddAggregate is called only inside Expression visitor, and AddExpression is called each time final expression is created.
    /// The adding works as follows:
    /// 1. Aggregation is created and method AddAggregation is called.
    /// 2. A reference to that aggregation is created which defines an expression which is then Added via AddExpression.
    ///    The check whether there are called simple expression and aggregation when no group by is defined is done always with the AddExpression.
    ///    
    /// Class stores each expression into a list of expressions.
    /// When adding the group by hash expr, the hash expr is also an expression. Thus, it is added into lists of expressions and the list of hashes.
    /// Note that in order to make this class work properly, the group by object must be created first. Because this class assumes the 
    /// hash expression are already inside before adding other expressions from other clauses.
    /// </summary>
    internal class QueryExpressionInfo
    {
        /// <summary>
        /// Expressions used by group by to group results.
        /// </summary>
        public List<ExpressionHolder> groupByhashExprs { get; } = new List<ExpressionHolder>();
        /// <summary>
        /// Used aggregate functions for a group by.
        /// </summary>
        public List<Aggregate> aggregates { get; } = new List<Aggregate>();
        /// <summary>
        /// Every other expression used inside query.
        /// </summary>
        public List<ExpressionHolder> exprs { get; } = new List<ExpressionHolder>();
        public bool IsSetGroupBy { get; set; } = false;
    
        public QueryExpressionInfo(bool isSetGroupBy)
        {
            this.IsSetGroupBy = isSetGroupBy;
        }

        /// <summary>
        /// Checks whether an expression (no aggregation) is valid, mainly in terms of grouping expressions.
        /// That is to say, if the group by is set, the clause provides expressions to group the results with.
        /// This function should check, whether the expressions in other clauses are as same as the grouping exp.
        /// Because, only the same expressions can be referenced throughout the query + aggregates.
        /// 
        /// If no group by is set. And aggregation is referenced, then only aggregations can be referenced in the entire query.
        ///
        /// This function is very simplified becuase expressions contain only one block.
        /// Thus, it must be reimplemented in the future.
        /// </summary>
        /// <param name="holder"> An expression. </param>
        /// <returns> A position of the added expression.  </returns>
        public int AddExpression(ExpressionHolder holder)
        {
            // Only expressions from group by clause and aggregates can be used.
            if (this.IsSetGroupBy)
            {
                // Aggregates in expressions can be referenced freely
                if (!holder.ContainsAggregate())
                {
                    // There can be referenced only expressions from group by.
                    if (this.groupByhashExprs.IndexOf(holder) == -1) throw new ArgumentException($"{this.GetType()}, expression in the query can contain only references from group by clause.");
                    else return this.exprs.IndexOf(holder);
                } else return AddExpr(holder);
            } 
            else
            {
                // No group by is set.
                if ((holder.ContainsAggregate() && ContainsSimpleExpr()) || (!holder.ContainsAggregate() && this.aggregates.Count > 0))
                    throw new ArgumentException($"{this.GetType()}, there was references an aggregate and a simple expression while group by is not set.");
                else return AddExpr(holder);
            }
        }

        /// <summary>
        /// Adds an aggregate to common aggregate functions.
        /// And returns position where it was added or the position of the aggregate that has been already addeds.
        /// If it already contains the aggregate, it returns the position of the containing one.
        /// </summary>
        /// <param name="aggregate">An aggregate function. </param>
        public int AddAggregate(Aggregate aggregate)
        {
            int position = -1;
            if ((position = this.aggregates.IndexOf(aggregate)) != -1) return position;
            else
            {
                this.aggregates.Add(aggregate);
                return this.aggregates.Count - 1;
            }
        }

        /// <summary>
        /// Adds hash expression for group by.
        /// Cannot contain aggregations.
        /// Each hash can be added only once.
        /// </summary>
        /// <param name="holder"> An expression to hash with. </param>
        public void AddGroupByHash(ExpressionHolder holder)
        {
            if (holder.ContainsAggregate())
                throw new ArgumentException($"{this.GetType()}, group by clause cannot contain aggregates.");
            else if (this.groupByhashExprs.Contains(holder))
                throw new ArgumentException($"{this.GetType()}, group by clause cannot contain the same aggregate multiple times.");
            else 
            { 
                this.groupByhashExprs.Add(holder);
                this.exprs.Add(holder);
            }
        }

        /// <summary>
        /// Checks whether a simple expression was used in the query.
        /// </summary>
        /// <returns> True if contains simple exp, otherwise false. </returns>
        private bool ContainsSimpleExpr()
        {
            for (int i = 0; i < this.exprs.Count; i++)
                if (!this.exprs[i].ContainsAggregate()) return true;
            return false;
        }

        /// <summary>
        /// Tried to add expression. If it contains the expression, return its position.
        /// Otherwise add the expression and return its new position.
        /// </summary>
        /// <param name="holder"> An expression to add. </param>
        /// <returns> A position of the added expression. </returns>
        private int AddExpr(ExpressionHolder holder)
        {
            int position = -1;
            if ((position = this.exprs.IndexOf(holder)) != -1) return position;
            else
            {
                this.exprs.Add(holder);
                return this.exprs.Count - 1;
            }
        }

    }
}
