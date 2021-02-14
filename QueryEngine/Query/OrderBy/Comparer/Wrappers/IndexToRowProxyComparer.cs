using System;
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
        private RowComparer rowComparer;
        private ITableResults resTable;
        /// <summary>
        /// A flag whether to allow returning of 0 as the comparison result.
        /// </summary>
        public readonly bool allowDuplicities;

        public IndexToRowProxyComparer(RowComparer rowComparer, ITableResults resTable, bool allowDuplicities)
        {
            if (rowComparer == null || resTable == null)
                throw new ArgumentException($"{this.GetType()}, trying to assign null to a constructor.");

            this.rowComparer = rowComparer;
            this.resTable = resTable;
            this.allowDuplicities = allowDuplicities;
        }

        public override int Compare(int x, int y)
        {
            int compRes = this.rowComparer.Compare(this.resTable[x], this.resTable[y]);
            if (this.allowDuplicities) return compRes;
            else
            {
                if (compRes == 0) return x.CompareTo(y);
                else return compRes;
            }
        }
    }
}
