/*! \file 
 
    This file includes definition of a multi column version of a simple sorter.
    Multi column sorter sorts table of results when multiple columns occur in the result table.
    The sort is done via allocating array of indeces (0 to result count). The array is sorted
    and the resulting array represents the sorted elements of a table. The array is then added to the table.
    By sorting only indeces, it helps to speed up the process of swapping long rows in the result table.
    
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Represents version of a simple sorter -> Sorts rows in the results table.
    /// Is used when there are multiple columns in the results table and the group by is not used.
    /// Sorter takes creates array of indeces representing rows in the table and the indeces are 
    /// sorted instead of actual rows in the table.
    /// </summary>
    internal sealed class MultiColumnSorter : SimpleSorter
    {
        private readonly IndexToRowProxyComparer indexComparer;

        /// <summary>
        /// Constructs multi column sorter.
        /// It comprises of  row comparers wrapped inside an integer comparer.
        /// </summary>
        /// <param name="sortData"> Result table to sort. </param>
        /// <param name="rowComparers"> Comparers for comparing rows in the table. </param>
        /// <param name="inParallel"> Flag is the table should be sorted in parallel. </param>
        public MultiColumnSorter(ITableResults sortData, List<ResultRowComparer> rowComparers, bool inParallel) : base(sortData, rowComparers, inParallel)
        {
            this.indexComparer = new IndexToRowProxyComparer(new RowComparer(rowComparers), sortData);
        }

        /// <summary>
        /// Sorts the result table.
        /// An array of indeces representing actual positions in the table is created.
        /// The array is then sorted using only the indeces. This saves time because it doesnt need to
        /// swap long rows in the result table.
        /// </summary>
        /// <returns> Sorted result table. </returns>
        public override ITableResults Sort()
        {
            int[] order = new int[this.dataTable.RowCount];
            order.AscPopulate(0);

            this.ArraySort(order, this.indexComparer);

            this.dataTable.AddOrder(order);
            return this.dataTable;
        }

    }
}
