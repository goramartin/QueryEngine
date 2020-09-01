using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class IndexToRowProxyComparer : IComparer<int>
    {
        ResultRowComparer rowComparer;
        ITableResults results;

        public IndexToRowProxyComparer(ResultRowComparer rowComparer, ITableResults results)
        {
            this.rowComparer = rowComparer;
            this.results = results;
        }

        public int Compare(int x, int y)
        {
            return this.rowComparer.Compare(this.results[x], this.results[y]);  
        }
    }
}
