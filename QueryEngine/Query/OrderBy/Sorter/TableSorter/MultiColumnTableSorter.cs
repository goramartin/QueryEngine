/*! \file 
This file includes definition of a multi column version of a simple sorter.
Multi column sorter sorts table of results when multiple columns occur in the result table.
The sort is done via allocating array of indeces (0 to result count). The array is sorted
and the resulting array represents the sorted elements of a table. The array is then added to the table.
By sorting only indeces, it helps to speed up the process of swapping long rows in the result table.
 */

namespace QueryEngine
{
    /// <summary>
    /// Represents version of a simple sorter -> Sorts rows in the results table.
    /// Is used when there are multiple columns in the results table and the group by is not used.
    /// Sorter takes creates array of indeces representing rows in the table and the indeces are 
    /// sorted instead of actual rows in the table.
    /// </summary>
    internal sealed class MultiColumnTableSorter : TableSorter
    {
        private IndexToRowProxyComparer indexComparer;

        /// <summary>
        /// Constructs multi column sorter.
        /// It comprises of  row comparers wrapped inside an integer comparer.
        /// </summary>
        /// <param name="resTable"> Result table to sort. </param>
        /// <param name="expressionComparers"> Comparers for comparing rows in the table. </param>
        /// <param name="inParallel"> Flag is the table should be sorted in parallel. </param>
        public MultiColumnTableSorter(ITableResults resTable, ExpressionComparer[] expressionComparers, bool inParallel) : base(resTable, inParallel)
        {
            var rowComparer = new RowComparer(expressionComparers);
            rowComparer.SetCaching(!inParallel);
            this.indexComparer = new IndexToRowProxyComparer(rowComparer, resTable);
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
            int[] order = new int[this.resTable.RowCount];
            order.AscPopulate(0);

            this.ArraySort(order, this.indexComparer);

            this.resTable.AddOrder(order);
            return this.resTable;
        }

    }
}
