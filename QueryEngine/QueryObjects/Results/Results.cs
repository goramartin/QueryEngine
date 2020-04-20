/*! \file
  
  This file includes a class that hold results of query matcher. 
  
  Each result consists of certain number of elements, those are variables defined in PGQL match section.
  The number of elements in the result defines the number of columns. Each variable is stored inside its 
  specific column. The column that it pertains to is the number stored inside variable map of the query.
  That means, every column contains only the same variable (even types if they are defined).
  
  One result of the search can be look as an array of those elements, where the number of elements in the 
  array is the number of columns.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Base interface for all result classes.
    /// </summary>
    interface IResults : IEnumerable<Element[]>
    {
        int ColumnCount { get; }
        int Count { get; }
        Element this[int row, int column] { get; }
    }


    /// <summary>
    /// Interface for class that stores results from a match expression.
    /// </summary>
    interface IMatchResultStorage : IResults
    {
        void AddElement(Element element, int columnIndex, int threadIndex);

        int ThreadCount { get; }
    }

}
