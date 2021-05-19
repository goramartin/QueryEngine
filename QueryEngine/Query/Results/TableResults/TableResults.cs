/*! \file
This file includes a class that holds results from query matcher. 
  
Each result consists of certain number of elements, those are variables defined in PGQL match section.
The number of elements in the result defines the number of columns. (Each variable is stored inside its 
specific column.) The column that it pertains to is the number stored inside variable map of the query.
That means, every column contains only the same variables (even types if they are defined).
  
One result of the search can be seen as an array of those elements, where the number of elements in the 
array is the number of columns. The specific row can be access with an index or an enumeration, on these actions,
the RowProxy struct is returned, henceforward, it enables the user access row's columns.

Note that if the the query does not contain the variable defined in the match clauses. It is not stored.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Represents a table that consists of columns and rows.
    /// Each column represents one variable and each row represents an ordered set of those variables.
    /// Example, imagine we have match expression MATCH (x) -[e]- (y), then
    /// the table consists of three columns where the columns are elements representing variables x, e, y
    /// in the given order.
    /// If order must be defined on the table. The class is given an array of integers, where each single integer
    /// represents an index to the table. In other words, when we enumerate the class, the rows are returned based on 
    /// the indeces from the "order" array.
    /// The class enables to store a temporary row, that can be accessed via the row proxy, however
    /// the temporary row can be used only if the table was created with the constructor without passed table.
    /// Note that the results are stored by columns, that is to say, returning one row must be done through proxy class (RowProxy in another file).
    /// The table itself is implemented as a List of fixed sized arrays.
    /// </summary>
    internal partial class TableResults : ITableResults
    {
        /// <summary>
        /// A flag whether new elements can be added into the table.
        /// If the elements are added, it can cause undefined behaviour.
        /// </summary>
        public bool IsStatic { get; } = false;

        /// <summary>
        /// [column][block][position in block]
        /// </summary>
        private List<Element[]>[] resTable;
        private int[] order;

        /// <summary>
        /// A number of columns.
        /// </summary>
        public int ColumnCount => this.resTable.Length;
        /// <summary>
        /// A number of rows in the table. Might be zero even if the matched elements are set.
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// A number of elements that actually were matched, if the query needs only count and not results explicitly.
        /// </summary>
        public int NumberOfMatchedElements { get; private set; }

        /// <summary>
        /// The row that enables to add a temporary row and access it throught appropriate RowProxy struct.
        /// Note that it should not be used if there is order set.
        /// </summary>
        public Element[] temporaryRow { get; set; } = null;
        public int FixedArraySize { get; private set; }
        /// <summary>
        /// An array of indeces of variable that must be stored. The rest is omited.
        /// </summary>
        public int[] usedVars { get; private set; }

        /// <summary>
        /// An empty constructor for passing into group by results for streamed grouping.
        /// Flags static as true.
        /// </summary>
        public TableResults() { 
        }

        /// <summary>
        /// Gets results from a merged matcher results.
        /// If the constructor runs the temporary row cannot be used.
        /// Flags for dynamic.
        /// </summary>
        /// <param name="elements"> Merged matcher results. </param>
        /// <param name="count"> A number of matched elements even though they might not be stored in the table. </param>
        /// <param name="fixedArraySize"> A size of blocks that store the results. </param>
        /// <param name="wasStoringResults"> A flag whether the count is equal to the number of elements in the tables.</param>
        public  TableResults(List<Element[]>[] elements, int count, int fixedArraySize, bool wasStoringResults)
        {
            if (elements == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");
            else if (elements.Length < 1 || fixedArraySize < 1)
                throw new ArgumentException($"{this.GetType()}, number of columns or array size, cannot be <= 0. columns == {elements.Length}, array size == {fixedArraySize}.");
            else
            {
                this.resTable = elements;
                this.NumberOfMatchedElements = count;
                this.FixedArraySize = fixedArraySize;
                if (wasStoringResults) this.RowCount = count;
                this.IsStatic = true;
            }
        }

        /// <summary>
        /// Creates an empty instance with the specified number of columns.
        /// Flags static as false.
        /// </summary>
        public TableResults(int columnCount, int fixedArraySize, int[] usedVars)
        {
            if (columnCount < 1 || fixedArraySize < 1)
                throw new ArgumentException($"{this.GetType()}, number of columns or array size, cannot be <= 0. columns == {columnCount}, array size == {fixedArraySize}.");
            else if (usedVars == null || usedVars.Length == 0)
                throw new ArgumentException($"{this.GetType()}, cannot pass empty variable list.");
            else
            {
                this.usedVars = usedVars;
                this.FixedArraySize = fixedArraySize;
                this.resTable = new List<Element[]>[columnCount];
                // Unused columns will be set to null.
                for (int i = 0; i < this.usedVars.Length; i++)
                    this.resTable[this.usedVars[i]] = new List<Element[]>();
            }
        }

        public void StoreRow(Element[] row)
        {
            var posInBlock = this.RowCount % this.FixedArraySize;
            var block = this.RowCount / this.FixedArraySize;

            // Add a new block to each column.
            if (posInBlock == 0)
            {
                this.InitNewBlock();
            }
            
            for (int i = 0; i < this.usedVars.Length; i++)
            {
                var column = this.usedVars[i];
                this.resTable[column][block][posInBlock] = row[column];
            }
            this.RowCount++;
        }

        private void InitNewBlock()
        {
            for (int i = 0; i < this.usedVars.Length; i++)
                this.resTable[this.usedVars[i]].Add(new Element[this.FixedArraySize]);
        }

        public void StoreTemporaryRow()
        {
            this.StoreRow(this.temporaryRow);
        }

        /// <summary>
        /// Accesses a table of results.
        /// </summary>
        /// <param name="rowIndex"> A row of a table. </param>
        /// <returns> An element on a given position based on an index if the order is not defined. 
        /// Otherwise, the appropriate index placed in the order array on the provided index is taken.</returns>
        public RowProxy this[int rowIndex]
        {
            get
            {
                if (this.order == null) return new TableResults.RowProxy(this, rowIndex);
                else return new TableResults.RowProxy(this, this.order[rowIndex]);
            }
        }

        /// <summary>
        /// Lazy enumeration of rows in the table.
        /// </summary>
        /// <returns> A row proxy from a table. </returns>
        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            for (int i = 0; i < this.RowCount; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Adds order to the table.
        /// </summary>
        /// <param name="order"> An array containing integers as indeces to the enclosed result table. </param>
        public void AddOrder(int[] order)
        {
            if (order == null)
                throw new ArgumentNullException($"{this.GetType()}, cannot assign null as an order to a table.");

            this.order = order;
        }
    }
}
