using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// A wrapper class of the results table with a tree index.
    /// </summary>
    internal class TableResultsABTreeHalfStreamed : ITableResults
    {
        ABTree<int> indexTree;
        ITableResults resultTable;

        public TableResults.RowProxy this[int rowIndex]
        {
            get {
                int i = 0;
                foreach (var row in indexTree)
                {
                    if (i == rowIndex) return this.resultTable[row];
                    i++;
                }
                throw new IndexOutOfRangeException($"{this.GetType()}, index out of range.");
            }
        }

        public TableResultsABTreeHalfStreamed(ABTree<int> indexTree, ITableResults resultTable)
        {
            if (indexTree == null || resultTable == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");
            this.indexTree = indexTree;
            this.resultTable = resultTable;
        }

        public int NumberOfMatchedElements => this.resultTable.NumberOfMatchedElements;

        public int ColumnCount => this.resultTable.ColumnCount;

        public int RowCount => this.resultTable.RowCount;

        public Element[] temporaryRow
        {
            get { return this.resultTable.temporaryRow; }
            set { this.resultTable.temporaryRow = value; }
        }

        public void AddOrder(int[] order)
        {
           throw new ArgumentException($"{this.GetType()}, trying to assign order to a table with a tree index.");
        }

        public void StoreRow(Element[] row)
        {
            this.resultTable.StoreRow(row);
            this.indexTree.Insert(this.resultTable.RowCount - 1);
        }

        public void StoreTemporaryRow()
        {
            this.resultTable.StoreTemporaryRow();
        }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            foreach (var rowIndex in this.indexTree)
                yield return this.resultTable[rowIndex];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
