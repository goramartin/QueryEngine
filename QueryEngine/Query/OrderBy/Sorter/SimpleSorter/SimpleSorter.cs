/*! \file 
 
This file includes definition of a simple sorter.
Simple sorter is used in case when the group by is not used. In other words it sorts only rows
from the matching algorithm using hpc sharp methods.
    
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Represents sorter that sorts given results table by rows in the table.
    /// The given row comparers are used to compare the rows.
    /// </summary>
    internal abstract class SimpleSorter : Sorter
    {
        protected ITableResults dataTable;
        protected bool inParallel;

        protected SimpleSorter(ITableResults sortData, bool inParallel)
        {
            this.inParallel = inParallel;
            this.dataTable = sortData;
        }

        protected void ArraySort<T>(T[] arr, IComparer<T> comparer)
        {
            if (this.inParallel) HPCsharp.ParallelAlgorithm.SortMergePar(arr, comparer);
            else HPCsharp.Algorithm.SortMerge(arr, comparer);
        }

        protected void ListSort<T>(List<T> ls, IComparer<T> comparer)
        {
            if (this.inParallel) HPCsharp.ParallelAlgorithm.SortMergePar(ls, comparer);
            else HPCsharp.Algorithm.SortMerge(ls, comparer);
        }

    }
}
