using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class ABTreeHalfStreamedSorter : OrderByResultProcessor
    {



        public ABTreeHalfStreamedSorter(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount) 
            : base(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount) 
        {








        } 

        public override void Process(int matcherID, Element[] result)
        {
            throw new NotImplementedException();
        }




        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            throw new NotImplementedException();
        }




    }
}
