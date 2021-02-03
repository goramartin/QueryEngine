/*! \file
This file includes a base class for all expression nodes.
Each node has evaluation method that tries to evaluate the expression that returns bool and a value.
If the evaluation fails (missing property value on an element) it returns false, otherwise the value
can be found in the "out" argument.

Expressions are part of the pgql expressions, such as SELECT, ORDER BY, GROUP BY...
For example SELECT x, y, x.AGE MATCH (x) - (y);
The "x", "y", "x.AGE" in select clause are expression that are evaluated for every individual 
results of the query.

Expressions work as follows.
Accessing an expression is done via an expression holder, that lets the user evaluate the containing expression
and return its value.
Expression themself are forming a syntax tree. Where each node evaluates it self and returns information about
evaluation to its predecessor.
*/

using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Base class for every expression node.
    /// Serves only as a holder.
    /// </summary>
    internal abstract class ExpressionBase
    {
        /// <summary>
        /// It is equal to its position in the QueryExpressionInfo list.
        /// </summary>
        protected int ExprPosition { get; set; } = -1;

        protected ExpressionBase()
        {}
        
        /// <summary>
        /// Gets expression type.
        /// </summary>
        /// <returns> Type of expression. </returns>
        public abstract Type GetExpressionType();

        /// <summary>
        /// Returns a list of used variable references in the expression node.
        /// If the variable is already inside the list, the variable is not included.
        /// </summary>
        /// <param name="vars"> A list of already collected variables. </param>
        /// <returns> A list of collected variables, the same list as the one in func parameters.</returns>
        public abstract List<int> CollectUsedVars(List<int> vars);

        /// <summary>
        /// Returns whether the expression is an aggregate reference. 
        /// </summary>
        public abstract bool ContainsAggregate();

        public virtual void SetExprPosition(int exprPos)
        {
            this.ExprPosition = exprPos;
        }
    }

    /// <summary>
    /// Each expression node will implement this interface.
    /// It provides methods for individual type of result classes.
    /// </summary>
    /// <typeparam name="T"> Type of return value.</typeparam>
    internal abstract class ExpressionReturnValue<T> : ExpressionBase
    {
        public abstract bool TryEvaluate(in TableResults.RowProxy elements, out T returnValue);
        public abstract bool TryEvaluate(in Element[] elements, out T returnValue);
        public abstract bool TryEvaluate(in GroupByResultsList.GroupProxyList group, out T returnValue);
        public abstract bool TryEvaluate(in GroupByResultsBucket.GroupProxyBucket group, out T returnValue);
        public abstract bool TryEvaluate(in GroupByResultsArray.GroupProxyArray group, out T returnValue);
        public abstract bool TryEvaluate(in AggregateBucketResult[] group, out T returnValue);
    }
}
