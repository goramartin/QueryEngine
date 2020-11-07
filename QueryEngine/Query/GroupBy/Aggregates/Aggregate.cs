/*! \file
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

        public static Aggregate FactoryArrayType(string funcType, Type compType, ExpressionHolder holder = null)
        {
            if (funcType == "count" && compType == typeof(int)) return new ArrayCount(holder);
            else if (funcType == "max" && compType == typeof(int)) return new IntArrayMax(holder);
            else if (funcType == "max" && compType == typeof(string)) return new StrArrayMax(holder);
            else if (funcType == "min" && compType == typeof(int)) return new IntArrayMin(holder);
            else if (funcType == "min" && compType == typeof(string)) return new StrArrayMin(holder);
            else if (funcType == "avg" && compType == typeof(int)) return new IntArrayAvg(holder);
            else if (funcType == "sum" && compType == typeof(int)) return new IntArraySum(holder);
            else throw new ArgumentException($"Aggregate factory, trying to create a non existent aggregate. {funcType}, {compType}");
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

        #region ArrayStorageInterface
        public abstract void Apply(in TableResults.RowProxy row, int position);
        public abstract void MergeOn(int into, int from);
        public abstract void SetMergingWith(AggregateArrayResults resultsStorage2);
        public abstract void SetAggResults(AggregateArrayResults resultsStorage);
        public abstract void UnsetMergingWith();
        public abstract void UnsetAggResults();
        #endregion ArrayStorageInterface

        public abstract Type GetAggregateReturnType();
        public abstract string GetFuncName();

    }

    /// <summary>
    /// A base class extension.
    /// </summary>
    /// <typeparam name="T"> A return type of an aggregate function. </typeparam>
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

    /// <summary>
    /// An aggregate fucntion base class for computing on array like storage.
    /// It enables to set direct reference to the result storage values.
    /// This enables to omit a lot of casts to the appropriate types.
    /// The methods SET stores the appropriate reference from the result storage holder.
    /// The methods UNSET unsets these references.
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregate function. </typeparam>
    internal abstract class AggregateArray<T> : Aggregate<T>
    {
        protected List<T> aggResults = null;
        protected List<T> mergingWithAggResults = null;

        public AggregateArray(ExpressionHolder expressionHolder) : base(expressionHolder)
        {}

        public override void SetMergingWith(AggregateArrayResults resultsStorage2)
        {
            this.mergingWithAggResults = ((AggregateArrayResults<T>)resultsStorage2).values;
        }

        public override void SetAggResults(AggregateArrayResults resultsStorage1) 
        {
            this.aggResults = ((AggregateArrayResults<T>)resultsStorage1).values;
        }

        public override void UnsetAggResults()
        {
            this.aggResults = null;
        }

        public override void UnsetMergingWith()
        {
            this.mergingWithAggResults = null;
        }
    }


}
