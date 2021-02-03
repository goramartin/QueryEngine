using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a results for every mather during the search.
    /// Instead of using simple lists to store results. It stores results in a fixed array
    /// sizes that do not need a resizing when the result table is full.
    /// This way, we can also omit memory/speed inefficient merging of the separate result tables.
    /// </summary>
    internal class MatchInternalFixedResults
    {
        private MatcherFixedResults[] matcherResults;
        /// <summary>
        /// A number of variable in the query.
        /// </summary>
        public int ColumnCount { get; private set; }
        /// <summary>
        /// A size of the arrays used for blocks.
        /// </summary>
        public int FixedArraySize { get; private set; }
        /// <summary>
        /// A number of results found  (Migh be set even if the results are not stored in resTable).
        /// </summary>
        public int NumberOfMatchedElements { get; set; }
        /// <summary>
        ///  Contains the final merged results of the search.
        ///  [x = column ][y  = block ][z = position in a block ]
        /// </summary>
        public List<Element[]>[] FinalMerged { get; private set; }

        public MatchInternalFixedResults(int arraySize, int columnCount, int threadCount)
        {
            this.matcherResults = new MatcherFixedResults[threadCount];
            for (int i = 0; i < threadCount; i++)
                this.matcherResults[i] = new MatcherFixedResults(arraySize, columnCount);

            this.ColumnCount = columnCount;
            this.FixedArraySize = arraySize;

            this.FinalMerged = new List<Element[]>[this.ColumnCount];
        }

        public MatcherFixedResults GetMatcherResultsStorage(int i)
        {
            return this.matcherResults[i];
        }

        /// <summary>
        /// Called only in case where the query runs in single thread.
        /// </summary>
        public void MergeAllColumns()
        {
            for (int i = 0; i < this.ColumnCount; i++)
                this.MergeColumn(i);
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
            
            if (lastColumnBlocks.Count != 0)
            {
                lastColumnBlocks.Sort(Comparer<Tuple<Element[], int>>.Create((i1, i2) => i1.Item2.CompareTo(i2.Item2)));
                int firstAfterEmpty = MergeLastBlocks(lastColumnBlocks);
                for (int i = lastColumnBlocks.Count - 1; firstAfterEmpty <= i; i--)
                {
                    // Try if the last block is at least half full otherwise reallocate it.
                    if (i == firstAfterEmpty) columnBlocks.Add(TryReallocLastCopiedBlock(lastColumnBlocks[i].Item1, GetSizeOfLastBlocks(lastColumnBlocks)));
                    else columnBlocks.Add(lastColumnBlocks[i].Item1);
                }
            }
            this.FinalMerged[columnIndex] = columnBlocks;
        }

        /// <summary>
        /// Reallocated the last copied block from the merge of last blocks if it is half empty.
        /// </summary>
        private Element[] TryReallocLastCopiedBlock(Element[] lastBlock, int sumOfLastBlocks)
        {
            // The last block will have the size of the block that is left after all others are full.
            var lastBlockSize = sumOfLastBlocks % this.FixedArraySize;

            // If it is lower than 50%. 
            if (lastBlockSize != 0 && lastBlockSize < (this.FixedArraySize / 2))
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
                    if (j + 1 == rTable.Count && this.FixedArraySize != this.matcherResults[i].currentPosition)
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
            int i = 0;
            int j = blocks.Count - 1;
            var arr1 = blocks[i].Item1;
            var c1 = blocks[i].Item2 - 1;
            var arr2 = blocks[j].Item1;
            var c2 = blocks[j].Item2;
            while (i != j)
            {
                CopyFromBackToEnd(arr1, arr2, ref c1, ref c2);
                if (c1 < 0 && c2 >= this.FixedArraySize)
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
            for (; 0 <= c1 && c2 < this.FixedArraySize; c1--, c2++)
            {
                arr2[c2] = arr1[c1];
                arr1[c1] = null;
            }
        }

        /// <summary>
        /// Class that stores results of the matcher in a fixed sized arrays, instead of using simple List that 
        /// needs resizing everytime it exceeds the maximum capacity.
        /// </summary>
         public class MatcherFixedResults
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

            public MatcherFixedResults(int arraySize, int columnCount)
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
}
