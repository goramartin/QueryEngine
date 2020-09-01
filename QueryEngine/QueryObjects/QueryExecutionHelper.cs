/*! \file
 
 This file contains definition of a execution helper.
 Execution helper's job is to help with execution of a specific clauses of a query computation.
 Each query object adds interface to the helper that the object needs.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
 
    interface BaseExecutionHelper
    {
        /// <summary>
        /// Defines whether optional clause order by was defined in the user input query.
        /// </summary>
        bool IsSetOrderBy { get; set; }
        
        /// <summary>
        /// Number of threads that will be used during query execution.
        /// </summary>
        int ThreadCount { get; set; }

        /// <summary>
        /// If more than one thread must be used return true, otherwise false.
        /// </summary>
        bool IsParallel();  

    }

    interface MatchExecutionHelper : BaseExecutionHelper
    {
        /// <summary>
        /// If more than one thread is used to search,
        /// this defines number of vertices that will be distributed to threads during matching algorithm.
        /// </summary>
        int VerticesPerThread { get; set; }

        /// <summary>
        /// In cases where there are no optional pgql clauses, the matcher can omit merging of results to 
        /// speed up the returning of the results.
        /// </summary>
        /// <returns> True if other clauses were defined. </returns>
        bool IsMergeNeeded();

    }

    interface SelectExecutionHelper : BaseExecutionHelper
    {
        /// <summary>
        /// Type of printer for printing results.
        ///  Used inside print method for factory method of printer.
        /// </summary>
        string Printer { get; set; }
        /// <summary>
        /// Type of printing format.
        /// Used inside print method for factory method of formater.
        /// </summary>
        string Formater { get; set; }

        /// <summary>
        /// File name where to print results.
        /// </summary>
        string FileName { get; set; }

    }

    interface OrderByExecutionHelper : BaseExecutionHelper
    {
    }

    internal class QueryExecutionHelper : MatchExecutionHelper, SelectExecutionHelper, OrderByExecutionHelper
    {
        public int ThreadCount {get; set; }
        public bool IsSetOrderBy { get; set; }

        public int VerticesPerThread { get; set; }
        
        public string Printer {get; set;}
        public string Formater {get; set;}
        public string FileName {get; set; }

        public bool IsMergeNeeded()
        {
            return (this.IsSetOrderBy);
        }

        public bool IsParallel()
        {
            return this.ThreadCount == 1 ? false : true;
        }
     
        public QueryExecutionHelper()
        {

        }

    }
}
