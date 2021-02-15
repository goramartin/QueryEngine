/*! \file 
This file contains definitions of a proxy row struct.
Struct is readonly and contains only reference to a result table and index of a row.
Then it implements an indexer to provide access to each column of the row.
It is used during enumeration of results classes and as a argument to evaluation of expression.
*/

namespace QueryEngine
{
    internal partial class TableResults
    {
        /// <summary>
        /// Represents one row of a result table.
        /// Contains reference to the result table and an index of a row in the table.
        /// Each column of the row can be accessed with an indexer.
        /// </summary>
        public readonly struct RowProxy
        {
            private readonly TableResults resTable;
            public readonly int index;

            /// <summary>
            /// Constructs proxy row.
            /// </summary>
            /// <param name="results"> Result table. </param>
            /// <param name="index"> Index of a row in the given result table.</param>
            public RowProxy(TableResults results, int index)
            {
                this.index = index;
                this.resTable = results;
            }

            /// <summary>
            /// Accesses one column of the row.
            /// If the index points to beyond the table lists, then
            /// it assumes that the row is stored in the temporary field.
            /// </summary>
            /// <param name="column"> Index of a column. </param>
            /// <returns> Element in the given column.</returns>
            public Element this[int column]
            {
                get
                {
                    if (this.index == this.resTable.RowCount) return this.resTable.temporaryRow[column];
                    else
                    {
                        return this.resTable.resTable[column]
                                                     [this.index / this.resTable.FixedArraySize]  // Block
                                                     [this.index % this.resTable.FixedArraySize]; // Position in block
                    } 
                }
            }

            public int GetColumnCount()
            {
                return this.resTable.ColumnCount;
            }


            /// <summary>
            /// Returns string containing the index of the row and IDs of elements in the row.
            /// </summary>
            public override string ToString()
            {
                string tmpString =  "Row: " + this.index + " result: ";
                for (int i = 0; i < this.resTable.resTable.Length; i++)
                    tmpString += " " + this[i].ID.ToString();  
            
                return tmpString;
            }

            /// <summary>
            /// Checks whether used variables inside expression are same.
            /// In case there are the same, the expression should give the same 
            /// result.
            /// </summary>
            /// <param name="x"> First row. </param>
            /// <param name="y"> Second row.</param>
            /// <param name="usedVars"> Variables to compare. </param>
            /// <returns> True if all used variables are the same. </returns>
            public static bool AreIdenticalVars(in TableResults.RowProxy x, in TableResults.RowProxy y, int[] usedVars)
            {
                for (int i = 0; i < usedVars.Length; i++)
                    if (x[usedVars[i]].ID != y[usedVars[i]].ID) return false;

                return true;
            }
        }
    }
}
