﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A class representing multiple tables and their tree indeces.
    /// The class is used by the streamed order by.
    /// The enumeration over the tables/indeces is based on whether the indeces are supposed to be 
    /// read in the asc. or desc. order.
    /// </summary>
    internal class MultiTableResultsABTree : ITableResults
    {
        private ITableResults[] resTables;
        private bool isAsc;
        public bool IsStatic => true;
        public MultiTableResultsABTree(ITableResults[] resTables, bool isAsc) 
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
