using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A wrapper class of the results table with a tree accumulated index.
    /// </summary>
    internal class TableResultsABTreeAccum : ITableResults
    {
        private ABTreeValueAccumulator<int> indexTree;
        private ITableResults resTable;

        public bool IsStatic => false;

        public TableResults.RowProxy this[int rowIndex]
        {
            get
            {
                throw new IndexOutOfRangeException($"{this.GetType()}, cannot access indexer on a tree index.");
            }
        }

        public TableResultsABTreeAccum(ABTreeValueAccumulator<int> indexTree, ITableResults resultTable)
        {
            if (indexTree == null || resultTable == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");
            this.indexTree = indexTree;
            this.resTable = resultTable;
        }

        public int NumberOfMatchedElements => this.RowCount;

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
            foreach (var valueAccumulation in this.indexTree)
            {
                yield return this.resTable[valueAccumulation.value];
                var tmpAccums = valueAccumulation.accumulation;

                for (int i = 0; i < tmpAccums.Count; i++)
                {
                    yield return this.resTable[tmpAccums[i]];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
