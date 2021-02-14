using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{

    /// <summary>
    /// A wrapper class of the results table with a tree index.
    /// </summary>
    internal class TableResultsABTree : ITableResults
    {
        private ABTree<int> indexTree;
        private ITableResults resTable;

        public TableResults.RowProxy this[int rowIndex]
        {
            get {
                int i = 0;
                foreach (var row in indexTree)
                {
                    if (i == rowIndex) return this.resTable[row];
                    i++;
                }
                throw new IndexOutOfRangeException($"{this.GetType()}, index out of range.");
            }
        }

        public TableResultsABTree(ABTree<int> indexTree, ITableResults resultTable)
        {
            if (indexTree == null || resultTable == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");
            this.indexTree = indexTree;
            this.resTable = resultTable;
        }

        public int NumberOfMatchedElements => this.resTable.NumberOfMatchedElements;

        public int ColumnCount => this.resTable.ColumnCount;

        public int RowCount => this.resTable.RowCount;

        public Element[] temporaryRow
        {
            get { return this.resTable.temporaryRow; }
            set { this.resTable.temporaryRow = value; }
        }

        public void AddOrder(int[] order)
        {
           throw new ArgumentException($"{this.GetType()}, cannot add an order to the already sorted table..");
        }

        public void StoreRow(Element[] row)
        {
            this.resTable.StoreRow(row);
            this.indexTree.Insert(this.resTable.RowCount - 1);
        }

        public void StoreTemporaryRow()
        {
            this.resTable.StoreTemporaryRow();
        }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            foreach (var rowIndex in this.indexTree)
                yield return this.resTable[rowIndex];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
