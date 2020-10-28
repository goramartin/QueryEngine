using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// A base class for every query clause. Such as match, select...
    /// Enables to process query as a chain that contains processing units.
    /// Each unit calls Compute on the next and awaits results in the out parameter 
    /// of the Compute method.
    /// The chain is built so that units that do compute sooner are at the end of the chain.
    /// This enables to get rid of the processing unit after it completes the task.
    /// </summary>
    internal abstract class QueryObject
    {
        /// <summary>
        /// Processing unit that needs to finish before this one.
        /// </summary>
        protected QueryObject next;

        
        public abstract void Compute(out ITableResults results);

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="name"> A name of the clause to build. </param>
        /// <param name="graph"> A graph to conduct computation on. </param>
        /// <param name="helper"> A helper that contains information about execution. </param>
        /// <param name="map"> A map of variables. </param>
        /// <param name="parseTree"> A parsed tree to create the clause from. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public static QueryObject Factory(Type type, Graph graph, QueryExecutionHelper helper, VariableMap map, Node parseTree, QueryExpressionInfo exprInfo)
        {
            if (type == typeof(SelectObject)) return new SelectObject(graph, map, helper, (SelectNode)parseTree, exprInfo);
            else if (type == typeof(MatchObject)) return new MatchObject(graph, map, helper, (MatchNode)parseTree, exprInfo);
            else if (type == typeof(OrderByObject)) return new OrderByObject(graph, map, helper, (OrderByNode)parseTree, exprInfo);
            else if (type == typeof(GroupByObject)) return new GroupByObject(graph, map, helper, (GroupByNode)parseTree, exprInfo);
            else throw new ArgumentException($"Query object factory, cannot create type {type.ToString()}.");
        }

        public void AddToEnd(QueryObject queryObject)
        {
            if (this.next == null) this.next = queryObject;
            else this.next.AddToEnd(queryObject);
        }
    }
}
