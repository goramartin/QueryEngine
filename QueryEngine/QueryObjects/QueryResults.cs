

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Interface for class that stores results of queries.
    /// </summary>
    interface IResultStorage : IEnumerable<Element[]>
    {
        void AddElement(Element element, int columnIndex, int threadIndex);

        int ColumnCount { get; }
        int ThreadCount { get; }
    }


    /// <summary>
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
    class QueryResults : IResultStorage
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
        /// Creates storage based on thread count and column count.
        /// Column count represents number of variables of a search query and
        /// thread count defines how many threads add results to this instance. 
        /// </summary>
        /// <param name="columnCount"> Number of variables in search query. </param>
        /// <param name="threadCount"> Number of threads that add results to this instance.</param>
        public QueryResults(int columnCount, int threadCount)
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
        }

        /// <summary>
        /// Adds element into specified column and thread index.
        /// </summary>
        /// <param name="element"> Element to be added.</param>
        /// <param name="columnIndex"> Index of a column. </param>
        /// <param name="threadIndex"> Index of a thread. </param>
        public void AddElement(Element element, int columnIndex, int threadIndex)
        {
            if (columnIndex < 0 || columnIndex >= this.ColumnCount) 
                throw new ArgumentException($"{this.GetType()}, Cannot add into column = {columnIndex}.");
            if (threadIndex < 0 || threadIndex >= this.ThreadCount) 
                throw new ArgumentException($"{this.GetType()}, Cannot add into thread index = {threadIndex}.");

            this.results[columnIndex][threadIndex].Add(element);
        }



        /// <summary>
        /// Enumeration over all the results.
        /// We are returning reference to a internal array in order to avoid unneccessary creation
        /// of a new arrays.
        /// </summary>
        /// <returns> Enumerator that returns an array that is held inside of the Enumerator. </returns>
        public IEnumerator<Element[]> GetEnumerator()
        {
            var result = new Element[this.ColumnCount];

            // For each thread
            for (int threadIndex = 0; threadIndex < this.ThreadCount; threadIndex++)
            {
                // For each result of the thread.
                // this.results[0][threadIndex].Count we take count on the first column, and we assume that the same count is
                // each columns. 
                for (int resultIndex = 0; resultIndex < this.results[0][threadIndex].Count; resultIndex++)
                {
                    // Collect elements from each column and put it inside array.
                    for (int columnIndex = 0; columnIndex < this.ColumnCount; columnIndex++)
                    {
                        result[columnIndex] = results[columnIndex][threadIndex][resultIndex];
                    }
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Calls generic methods.
        /// </summary>
        /// <returns> Generic Enumerator. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
