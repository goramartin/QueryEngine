using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A base interface for table result classes.
    /// Table results represent results from match query stored in a form of a table.
    /// </summary>
    internal interface ITableResults : IEnumerable<TableResults.RowProxy>
    {
        /// <summary>
        /// Contains a number of results matched by the matchers from the matching algorithm.
        /// The RowCount and this field might differ if the flag IsStoringResults from the execution helper 
        /// is set to false.
        /// </summary>
        int NumberOfMatchedElements { get; }
        int ColumnCount { get; }
        /// <summary>
        /// Contains a number of results directly in the table.
        /// </summary>
        int RowCount { get; }
        /// <summary>
        /// A row that can be accessed via a table even thought it is not stored directly in the table.
        /// </summary>
        Element[] temporaryRow { get; set; }
        void StoreTemporaryRow();
        void StoreRow(Element[] row);
        TableResults.RowProxy this[int rowIndex] { get; }
        void AddOrder(int[] order);
        /// <summary>
        /// If set to True the methods for adding row cause undefined behaviour.
        /// </summary>
        bool IsStatic { get; }
    }
}
