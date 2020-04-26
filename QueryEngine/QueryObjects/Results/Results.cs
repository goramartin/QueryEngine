﻿/*! \file
  
  This file includes a class that hold results of query matcher. 
  
  Each result consists of certain number of elements, those are variables defined in PGQL match section.
  The number of elements in the result defines the number of columns. Each variable is stored inside its 
  specific column. The column that it pertains to is the number stored inside variable map of the query.
  That means, every column contains only the same variable (even types if they are defined).
  
  One result of the search can be look as an array of those elements, where the number of elements in the 
  array is the number of columns.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Base interface for all result classes.
    /// </summary>
    interface IResults : IEnumerable<RowProxy>
    {
        int ColumnCount { get; }
        int Count { get; }
        RowProxy this[int row] { get; }

    }

    /// <summary>
    /// Represents a table that consists of columns and rows.
    /// Each column represents one variable and each row represents a group of those variables.
    /// Example, imagine we have match expression (x) -[e]- (y), then
    /// the table consists of three columns where columns are elements representing variables x, e, y
    /// in the given order.
    /// Note that we store by columns, that is to say, returning one row must be done through proxy class.
    /// </summary>
    class Results : IResults
    {
        private List<Element>[] results;

        /// <summary>
        /// Number of columns.
        /// </summary>
        public int ColumnCount => this.results.Length;
        /// <summary>
        /// Number of rows in the table.
        /// </summary>
        public int Count => this.results[0].Count;


        /// <summary>
        /// Gets results from a merged matcher results and moves first row.
        /// It expects that the results will be merged in the first thread index.
        /// </summary>
        /// <param name="elements"> Matcher results. </param>
        public  Results(List<Element>[][] elements)
        {
            this.results = new List<Element>[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                this.results[i] = elements[i][0];
            }
        }

        /// <summary>
        /// Accesses table of results.
        /// </summary>
        /// <param name="row"> Row of a table. </param>
        /// <returns> Element on a given position. </returns>
        public RowProxy this[int row]
        {
            get
            {
                if (row < 0 || row >= this.Count) 
                    throw new ArgumentOutOfRangeException($"{this.GetType()}, row is out of range.");
                else return new RowProxy(this.results, row);
            }
        }


        /// <summary>
        /// Lazy enum.
        /// Copies one row into internal array. That array is rewritten during next iteration.
        /// </summary>
        /// <returns> One row of a table. </returns>
        public IEnumerator<RowProxy> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
                yield return new RowProxy(this.results, i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}