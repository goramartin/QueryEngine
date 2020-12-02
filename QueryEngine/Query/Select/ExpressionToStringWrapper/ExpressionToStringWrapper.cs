/*! \file
This file contains definitions of a expressionToStringWrappers.
These classes are used to obtain string representation of an expression value during printing.
There is a need to obtains string values from evaluated expressions. Because we cannot implicitly work with the 
generic types, there must be a middle medium, which is this generic class.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class serves as a medium for evaluating expressions and obtaining string representation of the computed value.
    /// From this class inherits generic class which does explicit call to a generic method for evaluation of the expression.
    /// </summary>
    internal abstract class ExpressionToStringWrapper: IExpressionToString
    {
        public static string ExpressionFailStringValue = "null";

        /// <summary>
        /// Expression to be evaluated.
        /// </summary>
        protected ExpressionHolder expressionHolder;

        /// <summary>
        /// Construsts wrapper.
        /// </summary>
        /// <param name="expressionHolder"> Expression. </param>
        public ExpressionToStringWrapper(ExpressionHolder expressionHolder)
        {
            this.expressionHolder = expressionHolder;
        }

        /// <summary>
        /// Evaluates expression and returns its string representation.
        /// </summary>
        /// <param name="elements"> One result of the search. </param>
        public abstract string GetValueAsString(in TableResults.RowProxy elements);
        public abstract string GetValueAsString(in GroupByResultsList.GroupProxyList group);
        public abstract string GetValueAsString(in GroupByResultsBucket.GroupProxyBucket group);
        public abstract string GetValueAsString(in GroupByResultsArray.GroupProxyArray group);

        /// <summary>
        /// Print variable factory. Creates specialised wrapper based on a given type.
        /// </summary>
        /// <param name="expressionHolder"> Expression. </param>
        /// <param name="typeofPrintVariable"> Type of print variable. (Same as the expression type). </param>
        /// <returns> Specialised print variable. </returns>
        public static ExpressionToStringWrapper Factory(ExpressionHolder expressionHolder, Type typeofPrintVariable)
        {
            if (typeofPrintVariable == (typeof(int)))
                return new ExpressionToStringWrapper<int>(expressionHolder);
            else if (typeofPrintVariable == (typeof(string)))
                return new ExpressionToStringWrapper<string>(expressionHolder);
            else if (typeofPrintVariable == typeof(double))
                return new ExpressionToStringWrapper<double>(expressionHolder);
            else throw new ArgumentException($"PrintVariable factory, unknown type passed to a print variable factory. Type = {typeofPrintVariable}.");
        }
    }

    /// <summary>
    /// Specialised wrapper.
    /// Does explicit call to evaluate the containing expression.
    /// </summary>
    /// <typeparam name="T"> Type of value that will be computed and printed. </typeparam>
    internal sealed class ExpressionToStringWrapper<T> : ExpressionToStringWrapper
    {
        // To avoid casting with holder.TryGetExpressionValue
        private ExpressionReturnValue<T> expr;

        /// <summary>
        /// Constructs specialised wrapper.
        /// </summary>
        /// <param name="expressionHolder">Expression to be evaluated.</param>
        public ExpressionToStringWrapper(ExpressionHolder expressionHolder) : base (expressionHolder)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        /// <summary>
        /// Calls evaluation of containing expression for a given search result.
        /// </summary>
        /// <param name="elements"> One result of a search. </param>
        /// <returns>Null on failed evaluation or string prepresentation of a evaluated expression.</returns>
        public override string GetValueAsString(in TableResults.RowProxy elements)
        {
            if (this.expr.TryEvaluate(elements, out T returnValue)) 
                return returnValue.ToString();
            else return ExpressionToStringWrapper.ExpressionFailStringValue;
        }

        public override string GetValueAsString(in GroupByResultsList.GroupProxyList group)
        {
            if (this.expr.TryEvaluate(group, out T returnValue))
                return returnValue.ToString();
            else return ExpressionToStringWrapper.ExpressionFailStringValue;
        }

        public override string GetValueAsString(in GroupByResultsBucket.GroupProxyBucket group)
        {
            if (this.expr.TryEvaluate(group, out T returnValue))
                return returnValue.ToString();
            else return ExpressionToStringWrapper.ExpressionFailStringValue;
        }

        public override string GetValueAsString(in GroupByResultsArray.GroupProxyArray group)
        {
            if (this.expr.TryEvaluate(group, out T returnValue))
                return returnValue.ToString();
            else return ExpressionToStringWrapper.ExpressionFailStringValue;
        }

        public override string ToString()
        {
            return this.expressionHolder.ToString();
        }

    }

}
