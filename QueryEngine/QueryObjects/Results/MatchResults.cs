/*! \file 
 
    This file contains definition of a result class that is used by a matcher to store its sub results of each
    working thread.
 
    Class behaves like a 2 dimensional array. Where first array contains columns, the second index contains 
    list of all results of a specific thread. That is to say, each thread stores results into its specific index.
*/ 


using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine

{
    /// <summary>
    /// This class is used only by a matcher during matching to store its semi results of the matching,
    /// the class is then converted into a normal result structure with general interface.
    /// Class for storign matcher results.
    /// Contains 2-dimensional array. 
    /// [x][y] x = column, y = thread number.
    /// Column count is based on the number of variables from VariableMap of the query.
    /// Thread number is based on threads per query.
    /// Each matcher (thread) stores continualy results into selected columns and inside the columns
    /// it stores the final result onto an index of the thread index.
    /// That means, each thread can non-blockingly store its own results into this structure.
    /// Also this class implements enumerable index, where each return is compacted into inner array.
    /// So copying of the contents of the array is recomended before next interation.
    /// Note this structure does not check validity of the stores.
    /// </summary>
    internal sealed class MatchResultsStorage 
    {

        /// <summary>
        /// [x][y] x = column, y = thread number
        /// </summary>
        private List<Element>[][] results;

        /// <summary>
        /// Number of threads that will be adding to the instance.
        /// </summary>
        public int ThreadCount { get; private set; }

        /// <summary>
        /// Size of one result.
        /// </summary>
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Number of results.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Creates storage based on thread count and column count.
        /// Column count represents number of variables of a search query and
        /// thread count defines how many threads add results to this instance. 
        /// </summary>
        /// <param name="columnCount"> Number of variables in search query. </param>
        /// <param name="threadCount"> Number of threads that add results to this instance.</param>
        public MatchResultsStorage(int columnCount, int threadCount)
        {
            if (columnCount <= 0 || threadCount <= 0)
                throw new ArgumentException($"{this.GetType()}, trying to create results with invalid columnx or thread number.");


            this.results = new List<Element>[columnCount][];

            for (int i = 0; i < columnCount; i++)
            {
                this.results[i] = new List<Element>[threadCount];
                for (int j = 0; j < threadCount; j++)
                {
                    this.results[i][j] = new List<Element>();
                }
            }

            this.ColumnCount = columnCount;
            this.ThreadCount = threadCount;

            for (int i = 0; i < ThreadCount; i++)
                this.Count += results[0][i].Count;

        }

        /// <summary>
        /// Adds element into specified column and thread index.
        /// </summary>
        /// <param name="element"> Element to be added.</param>
        /// <param name="columnIndex"> Index of a column. </param>
        /// <param name="threadIndex"> Index of a thread. </param>
        public void AddElement(Element element, int columnIndex, int threadIndex)
        {
            this.results[columnIndex][threadIndex].Add(element);
        }

        public List<Element>[][] GetResults()
        {
            return this.results;
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
                this.results[columnIndex][0].AddRange(this.results[columnIndex][i]);
                this.results[columnIndex][i].Clear();
            }
        }

        /// <summary>
        /// Merges two thread rows on given indeces
        /// </summary>
        /// <param name="first"> Row to merge into.</param>
        /// <param name="second"> Data are copied to the first row.</param>
        public void MergeRows(int first, int second)
        {
            for (int i = 0; i < this.ColumnCount; i++)
            {
                this.results[i][first].AddRange(this.results[i][second]);
                this.results[i][second].Clear();
            }
        }
    }

}
