using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A wrapper class for a row proxy comparer. 
    /// This wrapper is given to the order algorithm.
    /// During the ordering the order algorithm orders indeces of rows inside the result table 
    /// instead of ordering rows explicitly.
    /// </summary>
    internal sealed class IndexToRowProxyComparer : Comparer<int>
    {
        private readonly RowComparer rowComparer;
        private readonly ITableResults resTable;

        public IndexToRowProxyComparer(RowComparer rowComparer, ITableResults resTable)
        {
            this.rowComparer = rowComparer;
            this.resTable = resTable;
        }

        public override int Compare(int x, int y)
        {
            return this.rowComparer.Compare(this.resTable[x], this.resTable[y]);  
        }
    }
}
