using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A wrapper class for a row proxy comparer. 
    /// It is used inside ABTree. This comparer handles cases where the expression results
    /// are completely same but the tree does not support the same keys inside.
    /// Thus a index of the row is used as a unique identifier, since each row in the table exists only
    /// once.
    /// </summary>
    internal sealed class IndexToRowProxyComparerNoDup : Comparer<int>
    {
        private readonly RowComparer rowComparer;
        private readonly ITableResults results;

        public IndexToRowProxyComparerNoDup(RowComparer rowComparer, ITableResults results)
        {
            this.rowComparer = rowComparer;
            this.results = results;
        }

        public override int Compare(int x, int y)
        {
            var compRes = this.rowComparer.Compare(this.results[x], this.results[y]);
            if (compRes == 0) return x.CompareTo(y);
            else return compRes;
        }
    }
}
