using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A wrapper class for a row proxy comparer. 
    /// It is used inside ABTree during HS order by. This comparer handles cases where the expression results
    /// are completely the same but the tree does not support the same keys inside.
    /// Thus a index of the row is used as a unique identifier, since each row in the table exists only
    /// once.
    /// </summary>
    internal sealed class IndexToRowProxyComparerNoDup : Comparer<int>
    {
        private readonly RowComparer rowComparer;
        private readonly ITableResults resTable;

        public IndexToRowProxyComparerNoDup(RowComparer rowComparer, ITableResults resTable)
        {
            this.rowComparer = rowComparer;
            this.resTable = resTable;
        }

        public override int Compare(int x, int y)
        {
            var compRes = this.rowComparer.Compare(this.resTable[x], this.resTable[y]);
            if (compRes == 0) return x.CompareTo(y);
            else return compRes;
        }
    }
}
