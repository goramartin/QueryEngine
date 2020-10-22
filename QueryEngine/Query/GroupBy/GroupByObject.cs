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
        private AggregationInfo aggregationInfo;

        /// <summary>
        /// Creates group by object.
        /// </summary>
        /// <param name="graph"> Property graph. </param>
        /// <param name="map"> Variable map. </param>
        /// <param name="executionHelper"> Select execution helper. </param>
        /// <param name="selectNode"> Parsed tree of select expression. </param>
        public GroupByObject(Graph graph, VariableMap map, IGroupByExecutionHelper executionHelper, GroupByNode groupByNode)
        {
            throw new NotImplementedException();
        }

        public override void Compute(out ITableResults results)
        {
            throw new NotImplementedException();
        }
    }
}
