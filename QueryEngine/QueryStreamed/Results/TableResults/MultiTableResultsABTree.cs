using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// A class representing multiple tables and an multiple tree indeces on the tables.
    /// The class is used by the streamed order by.
    /// The iteration over the tables/indeces is based on whether the indeces are supposed to be 
    /// read in the asc. or desc. order.
    /// </summary>
    internal class MultiTableResultsABTree : ITableResults
    {
        private TableResultsABTree[] resTables;
        private bool isAsc;

        public MultiTableResultsABTree(TableResultsABTree[] resTables, bool isAsc) 
        {
            if (resTables == null || resTables.Length == 0)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");
            this.resTables = resTables;
            this.isAsc = isAsc;
        }

        public TableResults.RowProxy this[int rowIndex] => throw new NotImplementedException();

        public int NumberOfMatchedElements => this.RowCount;

        public int ColumnCount => resTables[0].ColumnCount;

        public int RowCount
        {
            get
            {
                int rowCount = 0;
                for (int i = 0; i < this.resTables.Length; i++)
                    rowCount += this.resTables[0].RowCount;
                return rowCount;
            } 
        }

        public Element[] temporaryRow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            if (isAsc)
            {
                for (int i = 0; i < this.resTables.Length; i++)
                {
                    foreach (var item in this.resTables[i])
                        yield return item;
                }
            } else
            {
                for (int i = this.resTables.Length-1; i >= 0 ; i--)
                {
                    foreach (var item in this.resTables[i])
                        yield return item;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void AddOrder(int[] order)
        {
            throw new NotImplementedException();
        }


        public void StoreRow(Element[] row)
        {
            throw new NotImplementedException();
        }

        public void StoreTemporaryRow()
        {
            throw new NotImplementedException();
        }

    }
}
