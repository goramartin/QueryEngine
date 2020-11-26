﻿using System;
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
    /// instead of ordering rows explicitly. This class gives him interface for comparing the 
    /// indeces.
    /// </summary>
    internal sealed class IndexToRowProxyComparer : IComparer<int>
    {
        private readonly RowComparer rowComparer;
        private readonly ITableResults results;

        public IndexToRowProxyComparer(IExpressionComparer rowComparer, ITableResults results)
        {
            this.rowComparer = (RowComparer)rowComparer;
            this.results = results;
        }

        public int Compare(int x, int y)
        {
            return this.rowComparer.Compare(this.results[x], this.results[y]);  
        }
    }
}
