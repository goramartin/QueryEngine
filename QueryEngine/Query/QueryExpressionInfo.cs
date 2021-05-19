using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryEngine
{
    /// <summary>
    /// A class serves as an information collector of used expressions in a query.
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
    /// Class stores each expression into a List of expressions.
    /// When adding the group by hash expr, the hash expr is also an expression. Thus, it is added into lists of expressions and the list of hashes.
    /// Note that in order to make this class work properly, the group by object must be created first. Because this class assumes the 
    /// hash expressions are already inside before adding other expressions from other clauses.
    /// </summary>
    internal class QueryExpressionInfo
    {
        /// <summary>
        /// Expressions used by group by to group results.
        /// </summary>
        public List<ExpressionHolder> GroupByhashExprs { get; } = new List<ExpressionHolder>();
        /// <summary>
        /// Expressions used by order by to sort results.
        /// </summary>
        public List<ExpressionHolder> OrderByComparerExprs { get; } = new List<ExpressionHolder>();
        /// <summary>
        /// Used aggregate functions for a group by.
        /// </summary>
        public List<Aggregate> Aggregates { get; } = new List<Aggregate>();
        /// <summary>
        /// Every expression in the query except aggregates.
        /// </summary>
        public List<ExpressionHolder> Exprs { get; } = new List<ExpressionHolder>();
        public bool IsSetGroupBy { get; set; } = false;
    
        public QueryExpressionInfo(bool isSetGroupBy)
        {
            this.IsSetGroupBy = isSetGroupBy;
        }

        /// <summary>
        /// Checks whether an expression (no aggregation) is valid, in terms of grouping expressions.
        /// That is to say, if the group by is set, the clause provides expressions to group the results with.
        /// This function should check, whether the expressions in other clauses are the same as in the grouping exp.
        /// Because, only the same expressions can be referenced throughout the query + aggregates.
        /// 
        /// If no group by is set and aggregation is referenced, then only aggregations can be referenced in the entire query -> single group group by.
        ///
        /// This function is very simplified becuase expressions contain only one block.
        /// Thus, it must be reimplemented in the future.
        /// So far called only from Select clause parser.
        /// </summary>
        /// <param name="expressionHolder"> An expression. </param>
        /// <returns> Returns a position of the passed expression in the main expression list.  </returns>
        public int AddExpression(ExpressionHolder expressionHolder)
        {
            CheckNullExpression(expressionHolder);

            // Only expressions from group by clause and aggregates can be used.
            if (this.IsSetGroupBy)
            {
                // Aggregates in expressions can be referenced freely
                if (!expressionHolder.ContainsAggregate())
                {
                    // There can be referenced only expressions from group by.
                    if (this.GroupByhashExprs.IndexOf(expressionHolder) == -1) 
                        throw new ArgumentException($"{this.GetType()}, expression in the query can contain only references from group by clause.");
                    else return this.Exprs.IndexOf(expressionHolder);
                    // The check is not done, because the aggregate reference is create at the same time as the corresponding aggregate.
                } else return InsertExpr(expressionHolder);
            } 
            else
            {
                // No group by is set.
                if ((expressionHolder.ContainsAggregate() && ContainsSimpleExpr()) || (!expressionHolder.ContainsAggregate() && this.Aggregates.Count > 0))
                    throw new ArgumentException($"{this.GetType()}, there was references an aggregate and a simple expression while group by is not set.");
                else return InsertExpr(expressionHolder);
            }
        }

        /// <summary>
        /// Adds an aggregate to common aggregate functions.
        /// This does not throw because aggregates can be scattered across multiple clauses.
        /// </summary>
        /// <param name="aggregate"> An aggregate function. </param>
        /// <returns> Returns a position where it was added or the position of the aggregate that has been already added.</returns>
        public int AddAggregate(Aggregate aggregate)
        {
            CheckAggregateNull(aggregate);

            int position = -1;
            if ((position = this.Aggregates.IndexOf(aggregate)) != -1) return position;
            else
            {
                this.Aggregates.Add(aggregate);
                return this.Aggregates.Count - 1;
            }
        }

        /// <summary>
        /// Adds a hash expression for group by.
        /// Cannot contain aggregations.
        /// Each hash can be added only once.
        /// Note that this method is called only when the group by keys are created.
        /// Thus, hashes and expr have the same count.
        /// </summary>
        /// <param name="expressionHolder"> An expression to hash with. </param>
        /// <returns> Returns a position of the passed expression in the main expression list. </returns>
        public int AddGroupByHash(ExpressionHolder expressionHolder)
        {
            CheckNullExpression(expressionHolder);

            if (expressionHolder.ContainsAggregate())
                throw new ArgumentException($"{this.GetType()}, group by clause cannot contain aggregates.");
            else if (this.GroupByhashExprs.Contains(expressionHolder))
                throw new ArgumentException($"{this.GetType()}, group by clause cannot contain the same key multiple times.");
            else 
            {
                var pos = InsertExpr(expressionHolder);
                this.GroupByhashExprs.Add(this.Exprs[pos]);
                return pos;
            }
        }

        /// <summary>
        /// Adds a expression for order by.
        /// Cannot contain multiple occurrences of the same expression.
        /// </summary>
        /// <param name="expressionHolder"> An expression to sort with. </param>
        /// <returns> Returns a position of the passed expression in the main expression list. </returns>
        public int AddOrderByComp(ExpressionHolder expressionHolder)
        {
            CheckNullExpression(expressionHolder);

            if (this.OrderByComparerExprs.Contains(expressionHolder))
                throw new ArgumentException($"{this.GetType()}, order by clause cannot contain the same comparer expression multiple times.");
            else if (expressionHolder.ContainsAggregate())
                throw new ArgumentException($"{this.GetType()}, order by clause cannot contain aggregates.");
            else
            {
                var pos = InsertExpr(expressionHolder);
                this.OrderByComparerExprs.Add(this.Exprs[pos]);
                return pos;
            }
        }

        /// <summary>
        /// Checks whether a simple expression was used in the query.
        /// </summary>
        /// <returns> True if contains a simple exp, otherwise false. </returns>
        private bool ContainsSimpleExpr()
        {
            for (int i = 0; i < this.Exprs.Count; i++)
                if (!this.Exprs[i].ContainsAggregate()) return true;
            return false;
        }

        /// <summary>
        /// Tried to insert an expression to the general expr. list.
        /// </summary>
        /// <param name="expressionHolder"> An expression to add to the general expr. list. </param>
        /// <returns> If the main expression list contains the expression, return its position. Otherwise add the expression and return its new position. </returns>
        private int InsertExpr(ExpressionHolder expressionHolder)
        {
            CheckNullExpression(expressionHolder);

            int position = -1;
            if ((position = this.Exprs.IndexOf(expressionHolder)) != -1) return position;
            else
            {
                this.Exprs.Add(expressionHolder);
                expressionHolder.SetExprPosition(this.Exprs.Count - 1);
                return this.Exprs.Count - 1;
            }
        }

        private static void CheckNullExpression(ExpressionHolder expressionHolder)
        {
            if (expressionHolder == null || expressionHolder.Expr == null)
                throw new ArgumentNullException($"Query expression info, cannot store null expressions.");
        }

        private static void CheckAggregateNull(Aggregate aggregate)
        {
            if (aggregate == null)
                throw new ArgumentNullException($"Query expression info, cannot store null expressions.");
        }

        /// <summary>
        /// Returns indeces of variables used across the query.
        /// </summary>
        public int[] CollectUsedVariables()
        {
            List<int> vs = new List<int>();
            for (int i = 0; i < this.Exprs.Count; i++)
            {
                this.Exprs[i].CollectUsedVars(ref  vs);
            }
            
            vs.Sort();
            return vs.Distinct().ToArray();
        }
    }
}
