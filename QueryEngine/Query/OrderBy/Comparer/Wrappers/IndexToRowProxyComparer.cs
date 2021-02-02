using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        private readonly ITableResults results;

        public IndexToRowProxyComparer(RowComparer rowComparer, ITableResults results)
        {
            this.rowComparer = rowComparer;
            this.results = results;
        }

        public override int Compare(int x, int y)
        {
            return this.rowComparer.Compare(this.results[x], this.results[y]);  
        }
    }
}
