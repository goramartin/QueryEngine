using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A class is a wrapper class that contains multiple result tables and their global index.
    /// </summary>
    internal class MultiTableResultsRowProxyAccum : ITableResults
    {
        private List<ITableResults> resTables;
        private RowProxyAccum[] indexArray;
        public bool IsStatic => true;

        public MultiTableResultsRowProxyAccum(List<ITableResults> resTables, RowProxyAccum[] indexArray)
        {
            if (resTables == null || indexArray == null || resTables.Count == 0)
                throw new ArgumentNullException($"{this.GetType()}, passed null arguments to the constructor.");

            this.resTables = resTables;
            this.indexArray = indexArray;
        }

        public TableResults.RowProxy this[int rowIndex] => throw new ArgumentException($"{this.GetType()}, cannot access indexer on a tree index.");

        public int NumberOfMatchedElements => this.RowCount;

        public int ColumnCount => this.resTables[0].ColumnCount;
        public int RowCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < resTables.Count; i++)
                    count += resTables[i].RowCount;
                return count;
            }
        }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            for (int i = 0; i < this.indexArray.Length; i++)
            {
                yield return this.indexArray[i].row;

                var tmpRow = this.indexArray[i].row;
                var tmpAccum = this.indexArray[i].accumulations;
                for (int j = 0; j < tmpAccum.Count; j++)
                {
                    yield return tmpRow.resTable[tmpAccum[j]];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        public Element[] temporaryRow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
