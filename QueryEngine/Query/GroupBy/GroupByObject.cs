﻿/*! \file
This file contains a definition of a group by object.
A group by can be conducted multiple ways.
Firstly, based on IsParallel flag in the execution helper.

Then based on what grouping is desired.
1. 
If a group by clause is defined in the user input. Then the grouping is done
with the help of user inputted expression. And a Multigroup grouper is selected.
The results from result table are hashed and put into a Dictionary with corresponding
indeces that represent their aggregate results. If no aggregates are defined for the query. 
Only hashing is done.
2.
If no group by clause is defined but input query contains aggregates. 
Then the results table itself represents single group.
And a Singlegroup grouper is selected. In this case, only aggregates can be accessed through
out the query. No single expressions can be referrenced.
*/

using System;
using System.Collections.Generic;

namespace QueryEngine
{
    internal sealed class GroupByObject : QueryObject
    {
        private IGroupByExecutionHelper helper;
        private ExpressionHolder[] hashes;
        private List<Aggregate> aggregates;
        /// <summary>
        /// Creates a group by object for multigroup group by (defined group by clause).
        /// </summary>
        /// <param name="graph"> A property graph. </param>
        /// <param name="variableMap"> A variable map. </param>
        /// <param name="executionHelper"> A group by execution helper. </param>
        /// <param name="groupByNode"> A parsed tree of group by expression. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public GroupByObject(Graph graph, VariableMap variableMap, IGroupByExecutionHelper executionHelper, GroupByNode groupByNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || groupByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            this.helper = executionHelper;

            var groupbyVisitor = new GroupByVisitor(graph.labels, variableMap, exprInfo);
            groupbyVisitor.Visit(groupByNode);
            this.hashes = groupbyVisitor.GetResult().ToArray();

            this.aggregates = exprInfo.Aggregates;
            this.helper.IsSetGroupBy = true;
        }

        /// <summary>
        /// Creates a group by object for a single group group by (not defined group by clause but only aggregations in select).
        /// Assumes that the executionHelper.IsSetSingleGroupGroupBy is set to true.
        /// </summary>
        /// <param name="executionHelper"> A group by execution helper. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public GroupByObject(IGroupByExecutionHelper executionHelper, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || exprInfo == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            this.aggregates = exprInfo.Aggregates;
            this.helper = executionHelper;
        }

        public override void Compute(out ITableResults resTable, out GroupByResults groupByResults)
        {
            var aggs = this.aggregates.ToArray();

            if (next != null)
            {
                this.next.Compute(out resTable, out groupByResults);
                this.next = null;
                if (resTable == null) 
                    throw new ArgumentNullException($"{this.GetType()}, table results are set to null.");

                // If there are no results, return empty storage.
                if (resTable.NumberOfMatchedElements == 0)
                    groupByResults = new GroupByResultsList(new Dictionary<GroupDictKey, int>(), null, resTable);
                else
                {
                    Grouper grouper;
                    if (this.helper.IsSetSingleGroupGroupBy)
                        grouper = new SingleGroupGroupBy(aggs, null, this.helper);
                    else
                    {
                        // Use reference single thread solutions because the result table cannot be split equaly among threads.
                        // This also means that the result table is quite small.
                        if (resTable.NumberOfMatchedElements / helper.ThreadCount == 0)
                            grouper = Grouper.Factory(GrouperAlias.RefL, aggs, this.hashes, this.helper);
                        else grouper = Grouper.Factory(aggs, this.hashes, this.helper);
                    }
                    groupByResults = grouper.Group(resTable);
                }
            }
            else throw new NullReferenceException($"{this.GetType()}, next is set to null.");
        }
    }
}
