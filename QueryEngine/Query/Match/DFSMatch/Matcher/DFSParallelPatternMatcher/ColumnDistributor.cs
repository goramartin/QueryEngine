using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// The class serves as a work distributor to running threads that merge results from the result table.
    /// The threads call method to distribute column indeces that the threads will merge in parallel.
    /// </summary>
    internal sealed class ColumnDistributor
    {
        /// <summary>
        /// A number of columns that have been disributed.
        /// </summary>
        int firstFreeColumn = 0;
        /// <summary>
        /// A number of columns to distribute.
        /// </summary>
        readonly int columnCount;

        public ColumnDistributor(int columnCount)
        {
            this.columnCount = columnCount;
        }

        /// <summary>
        /// Distributes a free column index to merge.
        /// The method uses interlock atomic operation to avoid  using lock.
        /// It atomicaly increments the number of free column index.
        /// Then it substract the position and ther thread can decide whether to finish
        /// because all columns have been merged.
        /// </summary>
        /// <returns> Index of a column to merge or -1 on no more columns. </returns>
        public int DistributeColumn()
        {
            int tmpNextFreeColumn = Interlocked.Increment(ref this.firstFreeColumn);
            int tmpFirstFreeColumn = tmpNextFreeColumn - 1;

            if (tmpFirstFreeColumn < columnCount) return tmpFirstFreeColumn;
            else return -1;
        }

    }
}
