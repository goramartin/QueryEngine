using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A wapper class for a row proxy comparer.
    /// It is used during Half-Streamed order by that uses ABTreeValueAccumulator instead of a general ABTree.
    /// The RowProxyAccum values represent a row of a table that holds the same value as all the indeces in the in the accumulator.
    /// The indeces in the accumulator are the indeces corresponding to the table inside RowProxy.
    /// </summary>
    internal class RowProxyAccumToRowProxyComparer : Comparer<RowProxyAccum>
    {
        private RowComparer rowComparer;
        public RowProxyAccumToRowProxyComparer(RowComparer rowComparer)
        {
            if (rowComparer == null)
                throw new ArgumentException($"{this.GetType()}, trying to assign null to a constructor.");

            this.rowComparer = rowComparer;
        }

        public override int Compare(RowProxyAccum x, RowProxyAccum y)
        {
            return this.rowComparer.Compare(in x.row, in y.row);
        }
    }
}
