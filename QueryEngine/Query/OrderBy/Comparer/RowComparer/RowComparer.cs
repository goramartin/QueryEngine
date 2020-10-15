/*! \file 
 
This file contains definitions of a row comparer and a expression comparer.
    
Each row comparer contains a list of expression comparers.
During row comparing each expression comparer compares values computed with the given row.
Based on the result it decides whether to continue comparing expression or returns resulting value.
 
Expression comparer is given two rows and computes expression value of the both rows. The values are then compared
using templated compare methods. 

Null values in descenging order appear as last elements.
 
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueryEngine 
{

    /// <summary>
    /// Interface for comparing rows of a result table.
    /// </summary>
    internal interface IRowComparer
    {
        int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y);
    }

    /// <summary>
    /// Compares two rows.
    /// Contains list of all expression to compared with the rows.
    /// </summary>
    internal class RowComparer : IRowComparer
    {
        private readonly List<IRowComparer> comparers;
        
        
        /// <summary>
        /// Creates a row comparer.
        /// </summary>
        /// <param name="rowProxyComparers"> Expected a list of expression comparers.</param>
        public RowComparer(List<IRowComparer> rowProxyComparers)
        {
            this.comparers = rowProxyComparers;
        }

        /// <summary>
        /// Compares rows for every expression.
        /// If it find value !=0 then it will retrun the value. Otherwise it will continue comparing.
        /// </summary>
        /// <param name="x"> First row.</param>
        /// <param name="y"> Second row. </param>
        /// <returns> Less than zero x precedes y in the sort order.
        /// Zero x occurs in the same position as y in the sort order.
        /// Greater than zero x follows y in the sort order.</returns>
        public int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y)
        {
            int result = 0;
            for (int i = 0; i < this.comparers.Count; i++)
            {
                result = this.comparers[i].Compare(x, y);
                if (result != 0) return result;
            }
            return result;
        }
    }
}
