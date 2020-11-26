using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Represents a base interface for comparers that involve computing expression values.
    /// The methods should contain all the valid inputs to the expressions.
    /// </summary>
    internal interface IExpressionComparer
    {
        int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y);
    }

}
