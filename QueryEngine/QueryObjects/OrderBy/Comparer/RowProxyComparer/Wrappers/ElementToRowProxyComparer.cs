using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class ElementToRowProxyComparer : IComparer<Element>
    {
            IRowProxyComparer rowComparer;
            IResults results;

            public ElementToRowProxyComparer(IRowProxyComparer rowComparer, IResults results)
            {
                this.rowComparer = rowComparer;
                this.results = results;
            }


            public int Compare(Element x, Element y)
            {
            return 1;//rowComparer.Compare(results[x], results[y]);
            }
        
    }
}
