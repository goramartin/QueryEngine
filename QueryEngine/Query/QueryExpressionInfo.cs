﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class serves as a information collector of used expressions in a query.
    /// If the group by clause is defined. In the query, there can be referenced only
    /// the same expressions as in the group clause or an aggregate functions.
    /// How ever, if the group by clause is not in the query but aggregates are inputed.
    /// it still collects the aggregates. 
    /// 
    /// When collecting the aggregations and expressions, this class provides data to check
    /// against the correctness of the other expressions in the query.
    /// </summary>
    internal class QueryExpressionInfo
    {
        /// <summary>
        /// Expressions used by group by to group results.
        /// </summary>
        public readonly List<ExpressionHolder> groupByhashExprs = new List<ExpressionHolder>();
        /// <summary>
        /// Used aggregate functions for a group by.
        /// </summary>
        public readonly List<Aggregate> aggregates = new List<Aggregate>();
        /// <summary>
        /// Every other expression used inside query.
        /// </summary>
        public readonly List<ExpressionHolder> exprs = new List<ExpressionHolder>();
        public bool IsSetGroupBy { get; private set; } = false;
    
        /// <summary>
        /// Checks whether an expression (no aggregation) is valid, in terms of grouping expressions.
        /// That is to say, if the group by is set, the clause provides expressions to group the results with.
        /// This function should check, whether the expressions in other clauses are as same as the grouping exp.
        /// Because, only the same expression can be referenced throughout the query.
        /// 
        /// This function is very simplified becuase expressions contain only one block.
        /// Thus, it must be reimplemented in the future.
        /// </summary>
        /// <param name="holder"> An expression. </param>
        public void AddExpression(ExpressionHolder holder)
        {
            // Only expressions from group by clause and aggregates can be used.
            if (this.IsSetGroupBy)
            {
                // Aggregates in expressions can be referenced freely
                if (holder.ContainsAggregate()) this.exprs.Add(holder);
                else
                {
                    bool found = false;
                    for (int i = 0; i < this.groupByhashExprs.Count; i++)
                    {
                        if (holder.Equals(this.groupByhashExprs[i])) found = true;
                    }     
                    if (!found) throw new ArgumentException($"{this.GetType()}, expression in the query can contain only references from group by clause.");
                    else this.exprs.Add(holder);
                }
            } else
            {
                // No group by is set.
                // The expression can be added only if no aggregates are referenced.
                if (aggregates.Count != 0)
                    throw new ArgumentException($"{this.GetType()}, there was references an aggregate and a simple expression while group by is not set.");
                else this.exprs.Add(holder);
            }
        }

        /// <summary>
        /// Adds an aggregate to common aggregate functions.
        /// And returns position where it was added.
        /// </summary>
        /// <param name="aggregate">An aggregate function. </param>
        public int AddAggregate(Aggregate aggregate)
        {
            // If no group by is set but aggregate is referenced.
            // Only aggregates can be referenced.
            if (!this.IsSetGroupBy && this.exprs.Count != 0)
                    throw new ArgumentException($"{this.GetType()}, there was referenced an aggregate and no group by. In this case, only aggregates can be referenced.");

            if (this.TryFind(aggregate, out int position)) return position;
            else
            {
                this.aggregates.Add(aggregate);
                return this.aggregates.Count - 1;
            }
        }

        public void AddGroupByHash(ExpressionHolder holder)
        {
            this.groupByhashExprs.Add(holder);
        }

        private bool TryFind(Aggregate aggregate, out int position)
        {
            for (int i = 0; i < this.aggregates.Count; i++)
            {
                if (aggregate.Equals(this.aggregates[i])) 
                { 
                    position = i;
                    return true;
                }
            }
            position = default;
            return false;
        }
    }
}
