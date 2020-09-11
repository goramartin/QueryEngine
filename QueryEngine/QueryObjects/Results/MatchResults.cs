/*! \file 
This file contains definition of a result class that is used by a matcher to store its sub results of each
working thread.
 
Class behaves like a two dimensional array. Where first array contains columns (column is representing a single variable from the pgql match expression), the second index contains 
list of graph elements representing the varible of a column pertaining to a thread. That is to say, each thread stores results onto its specified index (the results of a thread are on the same row). Now, a row is formed 
by lists of results from a single thread.

*/ 

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// This class is used only by a matcher during matching to store its results,
    /// the class is then converted into a normal result table structure with a general interface.
    /// Results are contained in a 2-dimensional array. 
    /// [x][y] x = column, y = thread index.
    /// Column count is based on the number of variables from pgql match expression from the user.
    /// Thread number is based on available threads per query.
    /// Each matcher (a thread) stores continualy results into columns and inside the columns
    /// it stores the result onto an index of the thread index.
    /// That means, each thread can non-blockingly store its own results into this structure.
    /// Note this structure does not check validity of the stores.
    /// </summary>
    internal sealed class MatchResultsStorage 
    {
        /// <summary>
        /// [x][y] x = column, y = thread number
        /// </summary>
        private List<Element>[][] resTable;

        /// <summary>
        /// A number of results found  (Migh be set even if the results are not stored in resTable).
        /// </summary>
        public int NumberOfMatchedElements { get; set; }
        /// <summary>
        /// Number of threads that will be adding to the instance.
        /// </summary>
        public int ThreadCount { get; private set; }

        /// <summary>
        /// Size of one result.
        /// </summary>
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Defines whether the results have been merged into one row.
        /// Applies only if there are more threads used for matching.
        /// </summary>
        public bool IsMerged { get; set; } = false;

        /// <summary>
        /// Creates storage based on thread count and column count.
        /// Column count represents number of variables of a search query and
        /// thread count defines how many threads will be adding results to this instance. 
        /// </summary>
        /// <param name="columnCount"> Number of variables in search query. </param>
        /// <param name="threadCount"> Number of threads that add results to this instance.</param>
        public MatchResultsStorage(int columnCount, int threadCount)
        {
            if (columnCount <= 0 || threadCount <= 0)
                throw new ArgumentException($"{this.GetType()}, trying to create results with invalid column count or thread number.");

            // Init. columns of the result table.
            this.resTable = new List<Element>[columnCount][];

            // Each column contains an array of lists.
            for (int i = 0; i < columnCount; i++)
            {
                this.resTable[i] = new List<Element>[threadCount];
                for (int j = 0; j < threadCount; j++)
                    this.resTable[i][j] = new List<Element>(1024*(j+i));
            }

            this.ColumnCount = columnCount;
            this.ThreadCount = threadCount;
        }

        /// <summary>
        /// Adds element into specified column and thread index.
        /// </summary>
        /// <param name="element"> Element to be added. </param>
        /// <param name="columnIndex"> Index of a column. </param>
        /// <param name="threadIndex"> Index of a thread. </param>
        public void AddElement(Element element, int columnIndex, int threadIndex)
        {
            this.resTable[columnIndex][threadIndex].Add(element);
        }

        public List<Element>[][] GetResults()
        {
            return this.resTable;
        }

        public List<Element>[] GetThreadResults(int threadIndex)
        {
            List<Element>[] resultRow = new List<Element>[this.ColumnCount];

            for (int i = 0; i < this.ColumnCount; i++)
                resultRow[i] = this.resTable[i][threadIndex];

            return resultRow;
        }


        /// <summary>
        /// Merges results of a one column into the first thread index.
        /// And clears the rest.
        /// </summary>
        /// <param name="columnIndex"> Column index. </param>
        public void MergeColumn(int columnIndex)
        {
            for (int i = 1; i < this.ThreadCount; i++)
            {
                if (this.resTable[columnIndex][i] == null) continue;
                this.resTable[columnIndex][0].AddRange(this.resTable[columnIndex][i]);
                this.resTable[columnIndex][i] = null;
            }
        }

        /// <summary>
        /// Merges two thread rows on given indeces.
        /// </summary>
        /// <param name="first"> Row to merge into.</param>
        /// <param name="second"> Data are copied to the first row.</param>
        public void MergeRows(int first, int second)
        {
            for (int i = 0; i < this.ColumnCount; i++)
            {
                if (this.resTable[i][second] == null) continue;
                this.resTable[i][first].AddRange(this.resTable[i][second]);
                this.resTable[i][second] = null;
            }
        }
    }

}
