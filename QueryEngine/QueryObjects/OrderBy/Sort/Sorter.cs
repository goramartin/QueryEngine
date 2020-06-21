/*! \file 
 
 This file contains definition of a sorter.
 Sorter sorts results from a matching algorithm.
 So far it sorts only by rows.
 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    internal interface ISorter
    {
        IResults Sort();
    }

    /// <summary>
    /// Sorts rows of the given data table using given comparers.
    /// Sort is done via iterative quick sort with insertsort for certain threshold.
    /// </summary>
    internal class Sorter : ISorter
    {
        private IResults dataTable;
        private IRowProxyComparer rowComparer; 

        public Sorter(IResults sortData, List<IRowProxyComparer> rowComparers)
        {
            this.dataTable = sortData;
            this.rowComparer = new RowComparer(rowComparers);
        }

        public IResults Sort()
        {
            this.IterativeQuickWithInsertSort();
            return this.dataTable;
        }

        /// <summary>
        /// Sorts enclosed data table with iterative quick sort (no recursion).
        /// If the range passes certain thresh-hold it uses insertsort.
        /// </summary>
        private void IterativeQuickWithInsertSort()
        {
            Stack<int> partitionRanges = new Stack<int>();

            // Push initial range to the stack (entire range)
            partitionRanges.Push(0);
            partitionRanges.Push(this.dataTable.Count - 1);

            int high = 0;
            int low = 0;

            while (partitionRanges.Count > 0)
            {
                // Pop range from stack
                high = partitionRanges.Pop();
                low = partitionRanges.Pop();

                // If the range is smaller than the thresh hold use insert sort otherwise quicksort.
                if ((high - low + 1) < 10000) InsertSort(low, high);
                else
                {
                    // Set pivot of this range to the correct position.
                    int pivot = this.Partition(low, high);

                    // If there are elements on 
                    // left side of pivot, then 
                    // push left side to stack 
                    if (pivot - 1 > low)
                    {
                        partitionRanges.Push(low);
                        partitionRanges.Push(pivot - 1);
                    }

                    // If there are elements on 
                    // right side of pivot, then 
                    // push right side to stack 
                    if (pivot + 1 < high)
                    {
                        partitionRanges.Push(pivot + 1);
                        partitionRanges.Push(high);
                    }
                }
            }
        }

        /// <summary>
        /// Sorts range using insert sort.
        /// </summary>
        /// <param name="low"> Lower range bound. </param>
        /// <param name="high"> Upper range bound. </param>
        private void InsertSort(int low, int high)
        {
            for (int i = low; i < high; i++)
            {
                int j = i + 1;
                while (j > low && rowComparer.Compare(dataTable[j-1], dataTable[j]) > 0)
                {
                    this.dataTable.SwapRows(j, j - 1);
                    j--;
                }
            }
        }

        /// <summary>
        /// This function takes last element as pivot, 
        /// places the pivot element at its correct
        /// position in sorted array, and places all
        /// smaller elements(smaller than pivot) to left of
        /// pivot and all greater elements to right
        /// of pivot
        /// </summary>
        /// <param name="low"> Lower range bound.</param>
        /// <param name="high"> Upper range bound. </param>
        /// <returns> Position of a set pivot. </returns>
        private int Partition(int low, int high)
        {
            // index of smaller element 
            int topOfLowerPart = (low - 1);
            for (int i = low; i <= high - 1; i++)
            {
                // If current element is smaller 
                // than or equal to pivot (arr[i] <= pivot)
                if (rowComparer.Compare(dataTable[i], dataTable[high]) <= 0) 
                {
                    topOfLowerPart++;

                    // swap arr[topOfLowerPart] and arr[i]
                    this.dataTable.SwapRows(topOfLowerPart, i);
                }
            }

            // swap arr[topOfLowerPart+1] and arr[high] 
            // (or pivot) 
            this.dataTable.SwapRows(topOfLowerPart + 1, high);

            return topOfLowerPart + 1;
        }
    }
}
