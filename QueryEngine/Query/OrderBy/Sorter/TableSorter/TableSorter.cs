/*! \file 
This file includes a definition of a simple sorter.
The simple sorter is used in case when the group by is not used. In other words it sorts only rows
from the matching algorithm using hpc sharp methods.
 */
using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Represents a sorter that sorts given results table by rows in the table.
    /// The given row comparers are used to compare the rows.
    /// </summary>
    internal abstract class TableSorter : ISorter
    {
        protected ITableResults resTable;
        protected readonly bool inParallel;

        protected TableSorter(ITableResults resTable, bool inParallel)
        {
            if (resTable == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a construtor.");

            this.inParallel = inParallel;
            this.resTable = resTable;
        }

        protected static T[] ArraySort<T>(T[] arr, IComparer<T> comparer, bool inParallel)
        {
            if (inParallel) return HPCsharp.ParallelAlgorithm.SortMergePar(arr, comparer);
            else return HPCsharp.Algorithm.SortMerge(arr, comparer);
        }
    }
}
