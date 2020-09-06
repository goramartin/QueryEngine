using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class MultiTableResults : ITableResults
    {
        /// <summary>
        /// [x][y] x = column, y = thread number
        /// </summary>
        private List<Element>[][] resTables;
        
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

        public MultiTableResults(List<Element>[][] matchResults)
        {
            this.ColumnCount = matchResults.Length;
            this.ThreadCount = matchResults[0].Length;
            this.resTables = matchResults;
        }


        public TableResults.RowProxy this[int rowIndex] => throw new NotImplementedException();
        
        public void AddOrder(int[] order)
        {
            throw new NotImplementedException();
        }

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
