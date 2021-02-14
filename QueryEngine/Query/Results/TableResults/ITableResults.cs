using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base interface for table result classes.
    /// Table results represent results from match query stored in a form of a table.
    /// </summary>
    internal interface ITableResults : IEnumerable<TableResults.RowProxy>
    {
        int NumberOfMatchedElements { get; }
        int ColumnCount { get; }
        int RowCount { get; }
        Element[] temporaryRow { get; set; }
        void StoreTemporaryRow();
        void StoreRow(Element[] row);
        TableResults.RowProxy this[int rowIndex] { get; }
        void AddOrder(int[] order);
    }
}
