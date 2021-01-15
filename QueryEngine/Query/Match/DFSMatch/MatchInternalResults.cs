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

    /// <summary>
    /// Class represents a results for every mather during the search.
    /// Instead of using simple lists to store results. It stores results in a fixed array
    /// sizes that do not need a resizing when the result table is full.
    /// This way, we can also omit memory/speed inefficient mergin of the result tables.
    /// </summary>
    internal class MatchResultFixedContainer
    {
        public MatchResultsStorageFixed[] matcherResults;
        public int columnCount;
        public int fixedArraySize;
        /// <summary>
        ///  Contains the final merged results of the search.
        ///  [x = column ][y  = block ][z = position in a block ]
        /// </summary>
        public Element[][][] finalMerged; 

        public MatchResultFixedContainer(int arraySize, int columnCount, int threadCount)
        {
            this.matcherResults = new MatchResultsStorageFixed[threadCount];
            for (int i = 0; i < threadCount; i++)
                this.matcherResults[i] = new MatchResultsStorageFixed(arraySize, columnCount);

            this.columnCount = columnCount;
            this.fixedArraySize = arraySize;

            this.finalMerged = new Element[this.columnCount][][];
        }

        public MatchResultsStorageFixed GetMatcherResultsStorage(int i)
        {
            return this.matcherResults[i];
        }

        /// <summary>
        /// Merges a column from every matcher into one.
        /// It splits the blocks into blocks that are full and that are non full.
        /// The non full blocks are sorted by their capacity in ascending order.
        /// Then the blocks that have the least amount of elements inside are copied into blocks that have the most elements inside.
        /// When the process is finished, the sorted list now contains full blocks at the end, and empty blocks in the beginning.
        /// And it gets the index of the last block that was not emptied because the rest was full.
        /// </summary>
        public void MergeColumn(int columnIndex)
        {
            SplitToFullNonFull(columnIndex, out List<Element[]> columnBlocks, out List<Tuple<Element[], int>> lastColumnBlocks);
            // Sort the blocks according to their capacity in ascending order.
            lastColumnBlocks.Sort(Comparer<Tuple<Element[], int>>.Create((i1, i2) => i1.Item2.CompareTo(i2.Item2)));
            int firstAfterEmpty = MergeLastBlocks(lastColumnBlocks);
            // Copy the blocks that are full, and lastly the last non full block.
            for (int i = lastColumnBlocks.Count-1; firstAfterEmpty <= i; i--)
            {
                // Try if the last block is at least half full otherwise reallocate it.
                if (i == firstAfterEmpty) columnBlocks.Add(TryReallocLastCopiedBlock(lastColumnBlocks[i].Item1, GetSizeOfLastBlocks(lastColumnBlocks)));
                else columnBlocks.Add(lastColumnBlocks[i].Item1);
            }

            this.finalMerged[columnIndex] = columnBlocks.ToArray();
        }

        /// <summary>
        /// Reallocated the last copied block from the merge of last blocks if it is half empty.
        /// </summary>
        private Element[] TryReallocLastCopiedBlock(Element[] lastBlock, int sumOfLastBlocks)
        {
            // The last block will have the size of the block that is left after all others are full.
            var lastBlockSize = sumOfLastBlocks % this.fixedArraySize;

            // If it is lower than 50%. 
            if (lastBlockSize < (this.fixedArraySize / 2))
               Array.Resize(ref lastBlock, lastBlockSize);
            return lastBlock;
        }

        private int GetSizeOfLastBlocks(List<Tuple<Element[], int>> lastColumnBlocks)
        {
            int sum = 0;
            for (int i = 0; i < lastColumnBlocks.Count; i++)
                sum += lastColumnBlocks[i].Item2;
            return sum;
        }

        /// <summary>
        /// Splits blocks in the same columns from different matchers into block that are full and non full.
        /// </summary>
        private void SplitToFullNonFull(int columnIndex, out List<Element[]> columnBlocks, out List<Tuple<Element[], int>> lastColumnBlocks)
        {
            // Contain full blocks.
           columnBlocks = new List<Element[]>();
            // Contain non full blocks.
           lastColumnBlocks = new List<Tuple<Element[], int>>();

            // Split the blocks into full/non full.
            for (int i = 0; i < this.matcherResults.Length; i++)
            {
                var rTable = this.matcherResults[i].resTable;
                for (int j = 0; j < this.matcherResults[i].resTable.Count; j++)
                {
                    // Only last blocks can be non full.
                    if (j + 1 == rTable.Count && this.fixedArraySize != this.matcherResults[i].currentPosition)
                    {
                        // Because the currentPosition represents the first empty block, the block is empty with 0, or can be half full.
                        if (this.matcherResults[i].currentPosition == 0) continue;
                        else lastColumnBlocks.Add(Tuple.Create<Element[], int>(rTable[j][columnIndex], this.matcherResults[i].currentPosition));
                    }
                    else columnBlocks.Add(rTable[j][columnIndex]);
                }
            }
        }

        /// <summary>
        /// Merges last blocks.
        /// The passed array contains non full blocks in ascending order based on their capacity.
        /// The smaller arrays are copied into the bigger arrays.
        /// The indeces go from both sides and when they meet, the merging stops.
        /// </summary>
        /// <returns> Index of the block that was lastly copied. </returns>
        private int MergeLastBlocks(List<Tuple<Element[], int>> blocks)
        {
            int i =0; 
            int j = blocks.Count - 1;
            var arr1 = blocks[i].Item1;
            var c1 = blocks[i].Item2 - 1;
            var arr2 = blocks[j].Item1;
            var c2 = blocks[j].Item2;
            while (i != j)
            {
                CopyFromBackToEnd(arr1, arr2, ref c1, ref c2);
                if (c1 < 0 && c2 >= this.fixedArraySize)
                {
                    if (j - i == 1) return j;
                    i++;
                    arr1 = blocks[i].Item1;
                    c1 = blocks[i].Item2 - 1;
                    j--;
                    arr2 = blocks[j].Item1;
                    c2 = blocks[j].Item2;
                }
                else if (c1 < 0)
                {
                    i++;
                    arr1 = blocks[i].Item1;
                    c1 = blocks[i].Item2 - 1;
                }
                else
                {
                    j--;
                    arr2 = blocks[j].Item1;
                    c2 = blocks[j].Item2;
                }
            }
            return i;
        }

        /// <summary>
        /// From the end of the first array, copy elements to the end of the second array.
        /// </summary>
        private void CopyFromBackToEnd(Element[] arr1, Element[] arr2, ref int c1, ref int c2)
        {
            for (; 0 <= c1 && c2 < this.fixedArraySize; c1--, c2++)
            {
                arr2[c2] = arr1[c1];
                arr1[c1] = null;
            }
        }
    }

    /// <summary>
    /// Class that stores results of the matcher in a fixed sized arrays, instead of using simple List that 
    /// needs resizing everytime it exceeds the maximum capacity.
    /// </summary>
    internal class MatchResultsStorageFixed
    {
        public List<Element[][]> resTable;
        /// <summary>
        /// [x = column][y = row]
        /// </summary>
        public Element[][] lastBlock;
        /// <summary>
        /// Servers as a index of the current free position.
        /// </summary>
        public int currentPosition;
        /// <summary>
        /// A size of the array to store results.
        /// </summary>
        public int fixedArraySize;
        public int columnCount;

        public MatchResultsStorageFixed(int arraySize, int columnCount)
        {
            this.resTable = new List<Element[][]>();
            this.fixedArraySize = arraySize;
            this.columnCount = columnCount;
        }

        /// <summary>
        /// Adds a row to the table.
        /// If the table is full, it allocated another array to hold more values.
        /// </summary>
        public void AddRow(Element[] row)
        {
            // Enlarge
            if ((this.currentPosition) % this.fixedArraySize == 0)
            {
                Element[][] newBlock = new Element[this.columnCount][];
                for (int i = 0; i < this.columnCount; i++)
                    newBlock[i] = new Element[this.fixedArraySize];
                this.lastBlock = newBlock;
                this.resTable.Add(newBlock);
                this.currentPosition = 0;
            }

            // Insert
            for (int i = 0; i < this.columnCount; i++)
                this.lastBlock[i][this.currentPosition] = row[i];
            this.currentPosition++;
        }
    }

}
