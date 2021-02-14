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
        private List<TableResultsABTree> resTables;
        private bool isAsc;

        public MultiTableResultsABTree(List<TableResultsABTree> resTables, bool isAsc) 
        {
            if (resTables == null || resTables.Count == 0)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");
            this.resTables = resTables;
            this.isAsc = isAsc;
        }

        public TableResults.RowProxy this[int rowIndex] => throw new NotImplementedException();

        public int NumberOfMatchedElements => throw new NotImplementedException();

        public int ColumnCount => throw new NotImplementedException();

        public int RowCount => throw new NotImplementedException();

        public Element[] temporaryRow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AddOrder(int[] order)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
