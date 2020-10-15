using System;
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
        private QueryObject next;

        
        public abstract void Compute(out ITableResults results);

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="name"> A name of the clause to build. </param>
        /// <param name="graph"> A graph to conduct computation on. </param>
        /// <param name="helper"> A helper that contains information about execution. </param>
        /// <param name="map"> A map of variables. </param>
        /// <param name="parseTree"> A parsed tree to create the clause from. </param>
        /// <returns></returns>
        public static QueryObject Factory(string name, Graph graph, QueryExecutionHelper helper, VariableMap map, Node parseTree)
        {




            return null;
        }
    }
}
