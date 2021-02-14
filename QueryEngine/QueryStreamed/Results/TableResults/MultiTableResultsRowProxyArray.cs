using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{

    /// <summary>
    /// A class is a wrapper class that contains multiple result tables and their global index.
    /// </summary>
    internal class MultiTableResultsRowProxyArray : ITableResults
    {
        private List<ITableResults> resTables;
        private TableResults.RowProxy[] indexArray;

        public MultiTableResultsRowProxyArray(List<ITableResults> resTables, TableResults.RowProxy[] indexArray)
        {
            if (resTables == null || indexArray == null || resTables.Count == 0) 
                throw new ArgumentNullException($"{this.GetType()}, passed null arguments to the constructor.");

            this.resTables = resTables;
            this.indexArray = indexArray;
        }

        public TableResults.RowProxy this[int rowIndex] => this.indexArray[rowIndex];

        public int NumberOfMatchedElements => this.indexArray.Length;

        public int ColumnCount => this.resTables[0].ColumnCount;

        public int RowCount => this.indexArray.Length;

        public Element[] temporaryRow { 
            get => throw new ArgumentException($"{this.GetType()}, cannot get a temporary row in the sorted table."); 
            set => throw new ArgumentException($"{this.GetType()}, cannot store a temporary row in the already sorted table.");
        }

        public void AddOrder(int[] order)
        {
            throw new ArgumentException($"{this.GetType()}, cannot pass an order to the already sorted table.");
        }


        public void StoreRow(Element[] row)
        {
            throw new ArgumentException($"{this.GetType()}, cannot store a row in the already sorted table.");
        }

        public void StoreTemporaryRow()
        {
            throw new ArgumentException($"{this.GetType()}, cannot store a temporary row in the already sorted table.");
        }

        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            for (int i = 0; i < this.indexArray.Length; i++)
                yield return this.indexArray[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
