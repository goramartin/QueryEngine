/*! \file 

    This file contains definitions of a proxy row struct.
    Struct is readonly and contains only reference to a result table and index of a row.
    Then it implements an indexer to provide access to each column of the row.

    It is used during enumeration of results classes and as a argument to evaluation of expression.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    internal partial class Results
    {
        /// <summary>
        /// Represents one row of a result table.
        /// Contains reference to the result table and an index of a row in the table.
        /// Each column of the row can be accessed with an indexer.
        /// </summary>
        public readonly struct RowProxy
        {
            private readonly Results resTable;
            private readonly int index;

            /// <summary>
            /// Constructs proxy row.
            /// </summary>
            /// <param name="results"> Result table. </param>
            /// <param name="index"> Index of a row in the given result table.</param>
            public RowProxy(Results results, int index)
            {
                this.index = index;
                this.resTable = results;
            }

            /// <summary>
            /// Accesses one column of the row.
            /// </summary>
            /// <param name="column"> Index of a column. </param>
            /// <returns> Element in the given column.</returns>
            public Element this[int column]
            {
                get
                {
                    return resTable.results[column][this.index];
                }
            }

            public int GetColumnCount()
            {
                return this.resTable.results.Length;
            }

            /// <summary>
            /// Returns string containing the index of the row and IDs of elements in the row.
            /// </summary>
            public override string ToString()
            {
                string tmpString =  "Row: " + this.index + " result: ";
                for (int i = 0; i < this.resTable.results.Length; i++)
                    tmpString += " " + this[i].ID.ToString();  
            
                return tmpString;
            }



        }
    }
}
