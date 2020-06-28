/*! \file 
 
    This file includes definition of a single column sorter version of a simple sorter.
    Single column sorter sorts a list of elements from a matching algorithm without allocating 
    superfluous array like it is done in multi column simple sorter. This occurs only when the 
    results table contains only one column. 

    Simple sorter takes the one column from the results table and sorts it using hpc sharp methods.
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
    /// Is used when there is only one column in the results table and the group by is not used.
    /// Sorter takes the one column from the results table and sorts it using hpc sharp methods.
    /// </summary>
    internal sealed class SingleColumnSorter : SimpleSorter
    {
        /// <summary>
        /// Wrapper containing RowProxy comparer inside.
        /// </summary>
        private ElementToRowProxyComparer elementComparer;


        /// <summary>
        /// Constructs SingleColumnSorter.
        /// </summary>
        /// <param name="sortData"> Data table to sort. </param>
        /// <param name="rowComparers"> Comparers for comparing rows in the table. </param>
        /// <param name="inParallel"> Flag is the table should be sorted in parallel. </param>
        public SingleColumnSorter(IResults sortData, List<IRowProxyComparer> rowComparers, bool inParallel) : base(sortData, rowComparers, inParallel)
        {

            // to do create wrapper comparer 
        }

        /// <summary>
        /// Sorts 0th column from the results table.
        /// </summary>
        /// <returns> Sorted result table. </returns>
        public override IResults Sort()
        {
            List<Element> ls = (this.dataTable.GetResultColumn(0));
            this.ListSort<Element>(ls, this.elementComparer);
            return this.dataTable;
        }


    }
}
