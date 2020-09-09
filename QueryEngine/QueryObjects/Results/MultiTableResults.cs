/*! \file 
This file contains definitions of a multi table results.
This results table is used when merging results from the matching algorithm is not needed and 
can be immediately return for printing to a select expression.

The class obtains pure match results and because the data will be only enumerated the indexer and 
order can be left empty.

The enumeration works as follows.
It internally iterates over thread indeces.
So the enumeration starts with results only from the first thread and so on.
Each time the next thread results should be enumerated, the simple TableResult class is created from them.
And then the enumeration over the simpler class is started.
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A class is used for enumerating unmerged results from the matching algorithm.
    /// The indexer and order will never be used so they can be ommited.
    /// The enumeration is done by creating a simpler result table for each single thread,
    /// and start iteration over the simpler result table.
    /// </summary>
    internal class MultiTableResults : ITableResults
    {
        /// <summary>
        /// [x][y] x = column, y = thread number
        /// </summary>
        private List<Element>[][] resTables;
        
        public int Count { get; private set; }
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Number of threads that were active during matching algorithm.
        /// </summary>
        public int ThreadCount { get; private set; }

        public int RowCount
        { 
            get
            {
                int count = 0;
                for (int i = 0; i < resTables[0].Length; i++)
                {
                    count += resTables[0][i].Count;
                }
                return count;
            }
        }

        public MultiTableResults(List<Element>[][] matchResults, int count)
        {
            this.ColumnCount = matchResults.Length;
            this.ThreadCount = matchResults[0].Length;
            this.resTables = matchResults;
            this.Count = count;
        }


        public TableResults.RowProxy this[int rowIndex] => throw new NotImplementedException();
        
        public void AddOrder(int[] order)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Enumerate over results from each thread.
        /// </summary>
        public IEnumerator<TableResults.RowProxy> GetEnumerator()
        {
            for (int i = 0; i < this.ThreadCount; i++)
            {
                TableResults table = new TableResults(this.resTables, i);

                foreach (var row in table)
                    yield return row;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
