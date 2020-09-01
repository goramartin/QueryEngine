using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class MultiTableResults : ITableResults
    {
        public TableResults.RowProxy this[int rowIndex] => throw new NotImplementedException();

        public int ColumnCount => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public void AddOrder(int[] order)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public List<Element> GetResultColumn(int columnIndex)
        {
            throw new NotImplementedException();
        }

        public void SwapRows(int firstRowIndex, int secondRowIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
