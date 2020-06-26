﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class IndexToRowProxyComparer : IComparer<int>
    {
        IRowProxyComparer rowComparer;
        IResults results;

        public IndexToRowProxyComparer(IRowProxyComparer rowComparer, IResults results)
        {
            this.rowComparer = rowComparer;
            this.results = results;
        }


        public int Compare(int x, int y)
        {
            return rowComparer.Compare(results[x], results[y]);  
        }
    }
}
