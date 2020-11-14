﻿/*! \file
This file contains definition of an abstract aggregate function.
Each agg. function processes passed result row in its own way.
The aggregate contains expression holder to enable computation of the desired values.
The property IsAstCount serves as a helper to avoid unneccessary passing of rows into
count function. ( count(*) increases only the value, it doesnt have to compute the value)

There are two types of aggregates, the types stem from the way the results are stored.
The first one is array like storage and the second one is bucket like storage.

The array like storage:
The aggregates will also serve as a holders for the computed values (direct reference to the array result holders).
This enables to omit casting of the storage classes.
The Aggregate array results<> contains a list of immediate values that represent computes agg. values for a certain group. After the computation each index holds
the final computed value. In other words each index represents a single group.
 
Notice that the aggregate function compute the values only if the evaluated expression value
is not null.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
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
        protected ExpressionHolder expressionHolder;

        public Aggregate(ExpressionHolder expressionHolder)
        {
            this.expressionHolder = expressionHolder;
        }

        /// <summary>
        /// Creates a shallow copy.
        /// The expression is a shallow copy.
        /// The data stored inside the generic list are not coppied.
        /// Note that field IsAstCount is set based on the exp field.
        /// </summary>
        public Aggregate Clone()
        {
            return (Aggregate)Activator.CreateInstance(this.GetType(), this.expressionHolder);
        }

        /// <summary>
        /// Creates an aggregate that is bound with the array type results.
        /// </summary>
        /// <param name="funcName"> A name of the aggregate function.  </param>
        /// <param name="compType"> A return type of the aggregate function. </param>
        /// <param name="holder"> An expression to compute values with for the aggregate.</param>
        public static Aggregate Factory(string funcName, Type compType, ExpressionHolder holder = null)
        {
            if (funcName == "count" && compType == typeof(int)) return new Count(holder);
            else if (funcName == "max" && compType == typeof(int)) return new IntMax(holder);
            else if (funcName == "max" && compType == typeof(string)) return new StrMax(holder);
            else if (funcName == "min" && compType == typeof(int)) return new IntMin(holder);
            else if (funcName == "min" && compType == typeof(string)) return new StrMin(holder);
            else if (funcName == "avg" && compType == typeof(int)) return new IntAvg(holder);
            else if (funcName == "sum" && compType == typeof(int)) return new IntSum(holder);
            else throw new ArgumentException($"Aggregate factory, trying to create a non existent array bound aggregate. {funcName}, {compType}");
        }

        /// <summary>
        /// Creates an aggregate that is bound with the bucket type results.
        /// </summary>
        /// <param name="agg"> A aggregate to build from. </param>
        public static Aggregate Factory(Aggregate agg)
        {
            return Factory(agg.GetFuncName(), agg.GetAggregateReturnType(), agg.expressionHolder);
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (obj.GetType() != this.GetType()) return false;
            else
            {
                var tmp = (Aggregate)obj;
                if (this.expressionHolder == null && tmp.expressionHolder == null) return true;
                else if (this.expressionHolder.Equals(tmp.expressionHolder)) return true;
                else return false;
            }
        }
        public abstract Type GetAggregateReturnType();
        public abstract string GetFuncName();
       
        
        #region Buckets
        /// <summary>
        /// Is called only on aggregates that are bound with the bucket type results.
        /// It computes the desired value from the containing expression with the given row and applies it to the aggregate.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="bucket"> A position to apply the computed value into. </param>
        public abstract void Apply(in TableResults.RowProxy row, AggregateBucketResult bucket);
        /// <summary>
        /// A thread safe version of the simple apply method.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="bucket"> A position to apply the computed value into. </param>
        public abstract void ApplyThreadSafe(in TableResults.RowProxy row, AggregateBucketResult bucket);
        /// <summary>
        /// Merges results of two buckets into the bucket1.
        /// It assumes that the results were set before.
        /// </summary>
        /// <param name="bucket1"> A bucket that will contain the final merged results. </param>
        /// <param name="bucket2"> A bucket that will provide value to merge for the bucket1. </param>
        public abstract void Merge(AggregateBucketResult bucket1, AggregateBucketResult bucket2);

        /// <summary>
        /// Merges results of two buckets into the bucket1 with a thread safe manner.
        /// It assumes that the results were set before.
        /// </summary>
        /// <param name="bucket1"> A bucket that will contain the final merged results. </param>
        /// <param name="bucket2"> A bucket that will provide value to merge for the bucket1. </param>
        public abstract void MergeThreadSafe(AggregateBucketResult bucket1, AggregateBucketResult bucket2);



        #endregion Buckets

        #region Arrays
        
        /// <summary>
        /// Is called only on aggregates that are bound with the array type results.
        /// It computes the desired value from the containing expression with the given row and applies it to the aggregate.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="array"> A place to store results to. </param>
        /// <param name="position"> A position to apply the computed value into. </param>
        public abstract void ApplyThreadSafe(in TableResults.RowProxy row, AggregateArrayResults array, int position);

        #endregion Arrays

        #region Lists

        /// <summary>
        /// Is called only on aggregates that are bound with the list type results.
        /// It computes the desired value from the containing expression with the given row and applies it to the aggregate.
        /// </summary>
        /// <param name="row"> A result table row. </param>
        /// <param name="list"> A place to store results to. </param>
        /// <param name="position"> A position to apply the computed value into. </param>
        public abstract void Apply(in TableResults.RowProxy row, AggregateListResults list, int position);

        /// <summary>
        /// Is called during merging in LocalGroupLocalMerge grouping.
        /// It merges aggregates values from two different result holders and merges them into the
        /// first one on the "into" position.
        /// </summary>
        /// <param name="list1"> A place to store results to. </param>
        /// <param name="into"> The position to merge value into. </param>
        /// <param name="list2"> A place to merge results from. </param>
        /// <param name="from"> The position to merge value from. </param>
        public abstract void Merge(AggregateListResults list1, int into, AggregateListResults list2, int from);

        #endregion Lists

        #region InterCompatibility

        /// <summary>
        /// Merges results of a list into a bucket with a thread safe manner.
        /// It assumes that the results were set before.
        /// </summary>
        /// <param name="bucket"> A bucket that will contain the final merged results. </param>
        /// <param name="list"> A list that will provide value to merge for the bucket1. </param>
        /// <param name="position"> A position of a value in the list.</param>
        public abstract void MergeThreadSafe(AggregateBucketResult bucket, AggregateListResults list, int position);

        /// <summary>
        /// Merges results of a list into a bucket with a thread safe manner.
        /// It assumes that the results were set before.
        /// </summary>
        /// <param name="bucket"> A bucket that will contain the final merged results. </param>
        /// <param name="list"> A list that will provide value to merge for the bucket1. </param>
        /// <param name="position"> A position of a value in the list.</param>
        public abstract void Merge(AggregateBucketResult bucket, AggregateListResults list, int position);

        #endregion InterCompatibility
    }

    /// <summary>
    /// An aggregate fucntion base class.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregate function. </typeparam>
    internal abstract class Aggregate<T> : Aggregate
    {
        protected ExpressionReturnValue<T> expr;

        public Aggregate(ExpressionHolder expressionHolder) : base(expressionHolder)
        {
            if (expressionHolder != null) this.expr = (ExpressionReturnValue<T>)expressionHolder.Expr;
            else this.expr = null;
        }

        public override Type GetAggregateReturnType()
        {
            return typeof(T);
        }
    }
}
