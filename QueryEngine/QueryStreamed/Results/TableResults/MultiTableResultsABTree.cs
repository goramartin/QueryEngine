using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class MultiTableResultsABTree : ITableResults
    {





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
