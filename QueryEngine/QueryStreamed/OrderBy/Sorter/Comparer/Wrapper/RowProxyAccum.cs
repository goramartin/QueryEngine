using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Represents a struct with RowProxy and other accumulated values (indeces) of the table
    /// referenced by the row proxy.
    /// The struct is used in the Half-Streamed order by.
    /// </summary>
     internal readonly struct RowProxyAccum
     {
        public readonly TableResults.RowProxy row;
        public readonly List<int> accumulations;

        public RowProxyAccum(TableResults.RowProxy row, List<int> accum)
        {
            this.row = row;
            this.accumulations = accum;
        }
     }
}
