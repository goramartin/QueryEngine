/*! \file
This file contains definition of an abstract aggregate function.
Each agg. function processes passed result row in its own way.
The aggregate contains expression holder to enable computation of the desired values.
The property IsAstCount serves as a helper to avoid unneccessary passing of rows into
count function. ( count(*) increases only the value, it doesnt have to compute the value)

The aggregates will also serve as a holders for the computed values. The Aggregate<> contains
a list of immediate values during computation and after the computation each index holds
the final computed value. In other words each index represents a single group.
 
Notice that the aggregate function compute the values only if the evaluated expression value
is not null.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base class for all aggregate functions.
    /// </summary>
    internal abstract class Aggregate
    {
        /// <summary>
        /// A helper for accessing classes to distinguish basic count function.
        /// Because there is no need to pass and evaluate the expression.
        /// </summary>
        public bool IsAstCount { get; protected set; } = false;
        /// <summary>
        /// The expression is evaluated with a row from Apply method.
        /// </summary>
        public ExpressionHolder exp { get; }

        /// <summary>
        /// Computes expression value for a given row that is applied to the aggregate result.
        /// Each function handles its on computation and insertion.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="position"> A position of a groups aggregate. </param>
        public abstract void Apply(in TableResults.RowProxy row, int position);


        public static Aggregate Factory(string funcType, Type compType)
        {
            if (funcType == "count" && compType == typeof(int)) return new Count();
            else if (funcType == "max" && compType == typeof(int)) return new IntMax();
            else if (funcType == "max" && compType == typeof(string)) return new StrMax();
            else if (funcType == "min" && compType == typeof(int)) return new IntMin();
            else if (funcType == "min" && compType == typeof(string)) return new StrMin();
            else if (funcType == "avg" && compType == typeof(int)) return new IntAvg();
            else if (funcType == "sum" && compType == typeof(int)) return new IntSum();
            else throw new ArgumentException($"Aggregate factory, trying to create a non existent aggregate. {funcType}, {compType}");
        }
    }

    /// <summary>
    /// A base class extension.
    /// Adds a place to store aggregate values for each group.
    /// An index in the list represents one group and its associated aggregate value.
    /// </summary>
    /// <typeparam name="T"> A type of aggregate function. </typeparam>
    internal abstract class Aggregate<T> : Aggregate 
    {
        protected List<T> aggVals = new List<T>(2);
    }

}
