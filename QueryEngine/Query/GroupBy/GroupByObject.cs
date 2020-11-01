/*! \file

This file contains definition of a group by object.
A group by can be conducted multiple ways.
Firstly, based on IsParallel flag in the execution helper.

Then based on what grouping is desired.
1. 
If a group by clause is defined in the user input. Then the grouping is done
with the help of user inputted expression. And a Multigroup grouper is selected.
The results from result table are hashed and put into a dictionary with corresponding
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class GroupByObject : QueryObject
    {
        private IGroupByExecutionHelper helper;
        private List<ExpressionHolder> hashes;
        private List<Aggregate> aggregates;
        /// <summary>
        /// Creates group by object for multigroup results.
        /// </summary>
        /// <param name="graph"> Property graph. </param>
        /// <param name="variableMap"> Variable map. </param>
        /// <param name="executionHelper"> Select execution helper. </param>
        /// <param name="selectNode"> Parsed tree of select expression. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public GroupByObject(Graph graph, VariableMap variableMap, IGroupByExecutionHelper executionHelper, GroupByNode groupByNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || groupByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            this.helper = executionHelper;

            var groupbyVisitor = new GroupByVisitor(graph.labels, variableMap, exprInfo);
            groupbyVisitor.Visit(groupByNode);
            this.hashes = groupbyVisitor.GetResult();

            this.aggregates = exprInfo.aggregates;
            this.helper.IsSetGroupBy = true;
        }

        /// <summary>
        /// Creates group by object for single group result.
        /// </summary>
        /// <param name="executionHelper"> Select execution helper. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public GroupByObject(IGroupByExecutionHelper executionHelper, QueryExpressionInfo exprInfo)
        {
            this.aggregates = exprInfo.aggregates;
            this.helper = executionHelper;
        }

        public override void Compute(out ITableResults results)
        {
            if (next != null)
            {
                this.next.Compute(out results);
                this.next = null;
                Grouper grouper;
                if (this.helper.IsSetSingleGroupGroupBy) grouper = new SingleGroupGrouper(this.aggregates, null, this.helper);
                else grouper = new LocalGroupLocalMerge(this.aggregates, this.hashes, this.helper);
                grouper.Group(results);
            }
            else throw new NullReferenceException($"{this.GetType()}, next is set to null.");
        }
    }
}
