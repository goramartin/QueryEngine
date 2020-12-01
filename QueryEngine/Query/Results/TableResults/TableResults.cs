/*! \file
  
This file includes a class that hold results from query matcher. 
  
Each result consists of certain number of elements, those are variables defined in PGQL match section.
The number of elements in the result defines the number of columns. (Each variable is stored inside its 
specific column.) The column that it pertains to is the number stored inside variable map of the query.
That means, every column contains only the same variables (even types if they are defined).
  
One result of the search can be look as an array of those elements, where the number of elements in the 
array is the number of columns. The specific row can be access with an index or an enumeration, on these actions,
the RowProxy struct is returned, henceforward, it enables the user access row's columns.
  
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
    /// Base interface for table result classes.
    /// Table results represent results from match query stored in a form of a table.
    /// </summary>
    internal interface ITableResults : IEnumerable<TableResults.RowProxy>
    {
        int NumberOfMatchedElements { get; }
        int ColumnCount { get; }
        int RowCount { get; }
        TableResults.RowProxy this[int rowIndex] { get; }
        void AddOrder(int[] order);
    }

    /// <summary>
    /// Represents a table that consists of columns and rows.
    /// Each column represents one variable and each row represents an ordered set of those variables.
    /// Example, imagine we have match expression MATCH (x) -[e]- (y), then
    /// the table consists of three columns where the columns are elements representing variables x, e, y
    /// in the given order.
    /// If order must be defined on the table. The class is given an array of integers, where each single integer
    /// represents an index to the table. In other words, when we enumerate the class, the rows are returned based on 
    /// the indeces from the "order" array.
    /// Note that the results are stored by columns, that is to say, returning one row must be done through proxy class (RowProxy in another file).
    /// </summary>
    internal partial class TableResults : ITableResults
    {
        private readonly List<Element>[] resTable;
        private int[] order;

        /// <summary>
        /// Number of columns.
        /// </summary>
        public int ColumnCount => this.resTable.Length;
        /// <summary>
        /// Number of rows in the table. Might be zero even if the matched elements are set.
        /// </summary>
        public int RowCount => this.resTable[0].Count;

        /// <summary>
        /// Number of elements that actually were matched, if the query needs only count and not results explicitly.
        /// </summary>
        public int NumberOfMatchedElements { get; private set; }


        /// <summary>
        /// Gets results from a merged matcher results.
        /// It expects that the results will be merged in the first thread index.
        /// </summary>
        /// <param name="elements"> Matcher results merged on the zeroth index. </param>
        /// <param name="count"> Number of matched elements even though they might not be stored in the table. </param>
        public  TableResults(List<Element>[][] elements, int count)
        {
            this.order = null;
            this.resTable = new List<Element>[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                this.resTable[i] = elements[i][0];
            }
            this.NumberOfMatchedElements = count;
        }

        /// <summary>
        /// Gets results from a non merged matcher results.
        /// The index indicates which thread results should be picked to create the instance.
        /// </summary>
        /// <param name="elements"> Matcher results merged on the zeroth index. </param>
        /// <param name="threadNumber"> Number of a thread to pick results from. </param>
        /// <param name="count"> Number of matched elements even though they might not be stored in the table. </param>
        public TableResults(List<Element>[][] elements, int threadNumber, int count)
        {
            this.order = null;
            this.resTable = new List<Element>[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                this.resTable[i] = elements[i][threadNumber];
            }

            this.NumberOfMatchedElements = count;
        }

        /// <summary>
        /// Accesses table of results.
        /// </summary>
        /// <param name="rowIndex"> Row of a table. </param>
        /// <returns> Element on a given position based on an index if the order is not defined. 
        /// Otherwise, the appropriate index placed in the order array on the provided index is taken.</returns>
        public RowProxy this[int rowIndex]
        {
            get
            {
                if (order == null) return new TableResults.RowProxy(this, rowIndex);
                else return new TableResults.RowProxy(this, order[rowIndex]);
            }
        }

        /// <summary>
        /// Lazy enumeration of rows in the table.
        /// </summary>
        /// <returns> A row proxy from a table. </returns>
        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            for (int i = 0; i < this.RowCount; i++)
            {
                if (this.order == null) yield return new TableResults.RowProxy(this, i);
                else yield return new TableResults.RowProxy(this, order[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Adds order to the table.
        /// </summary>
        /// <param name="order"> Array containing integers as indeces to the enclosed result table. </param>
        public void AddOrder(int[] order)
        {
            this.order = order;
        }

    }

}
