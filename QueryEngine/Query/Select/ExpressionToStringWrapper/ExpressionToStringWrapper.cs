using System;

namespace QueryEngine
{
    /// <summary>
    /// Class serves as a medium for evaluating expressions and obtaining the string representation of the computed value.
    /// From this class inherits a generic class which does explicit call to a generic method for evaluation of the expression.
    /// </summary>
    internal abstract class ExpressionToStringWrapper: IExpressionToString
    {
        public static string ExpressionFailStringValue = "null";

        /// <summary>
        /// An expression to be evaluated.
        /// </summary>
        protected ExpressionHolder expressionHolder;
        
        public ExpressionToStringWrapper(ExpressionHolder expressionHolder)
        {
            if (expressionHolder == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

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
        public abstract string GetValueAsString(in AggregateBucketResult[] group);

        /// <summary>
        /// A factory method.
        /// </summary>
        /// <param name="expressionHolder"> An expression. </param>
        /// <param name="typeofPrintVariable"> A type of print variable. (Same as the expression type). </param>
        /// <returns> A specialised print variable on the provided type. </returns>
        public static ExpressionToStringWrapper Factory(ExpressionHolder expressionHolder, Type typeofPrintVariable)
        {
            if (typeofPrintVariable == (typeof(int)))
                return new ExpressionToStringWrapper<int>(expressionHolder);
            else if (typeofPrintVariable == (typeof(string)))
                return new ExpressionToStringWrapper<string>(expressionHolder);
            else if (typeofPrintVariable == typeof(double))
                return new ExpressionToStringWrapper<double>(expressionHolder);
            else if (typeofPrintVariable == typeof(long))
                return new ExpressionToStringWrapper<long>(expressionHolder);
            else throw new ArgumentException($"PrintVariable factory, unknown type passed to a print variable factory. Type = {typeofPrintVariable}.");
        }
    }

    /// <summary>
    /// A specialised wrapper.
    /// Does an explicit call to evaluate the containing expression.
    /// </summary>
    /// <typeparam name="T"> A type of value that will be computed and printed. </typeparam>
    internal sealed class ExpressionToStringWrapper<T> : ExpressionToStringWrapper
    {
        private ExpressionReturnValue<T> expr;

        public ExpressionToStringWrapper(ExpressionHolder expressionHolder) : base (expressionHolder)
        {
            this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
        }

        /// <summary>
        /// Calls evaluation of the containing expression for a given search result.
        /// </summary>
        /// <param name="elements"> One result of a search. </param>
        /// <returns> Null on failed evaluation or string prepresentation of a evaluated expression.</returns>
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

        public override string GetValueAsString(in AggregateBucketResult[] group)
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
