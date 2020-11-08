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

        /// <summary>
        /// Creates an aggregate that is bound with the array type results.
        /// </summary>
        /// <param name="funcName"> A name of the aggregate function.  </param>
        /// <param name="compType"> A return type of the aggregate function. </param>
        /// <param name="holder"> An expression to compute values with for the aggregate.</param>
        public static Aggregate FactoryArrayType(string funcName, Type compType, ExpressionHolder holder = null)
        {
            if (funcName == "count" && compType == typeof(int)) return new ArrayCount(holder);
            else if (funcName == "max" && compType == typeof(int)) return new IntArrayMax(holder);
            else if (funcName == "max" && compType == typeof(string)) return new StrArrayMax(holder);
            else if (funcName == "min" && compType == typeof(int)) return new IntArrayMin(holder);
            else if (funcName == "min" && compType == typeof(string)) return new StrArrayMin(holder);
            else if (funcName == "avg" && compType == typeof(int)) return new IntArrayAvg(holder);
            else if (funcName == "sum" && compType == typeof(int)) return new IntArraySum(holder);
            else throw new ArgumentException($"Aggregate factory, trying to create a non existent array bound aggregate. {funcName}, {compType}");
        }

        /// <summary>
        /// Creates an aggregate that is bound with the bucket type results.
        /// </summary>
        /// <param name="funcName"> A name of the aggregate function.  </param>
        /// <param name="compType"> A return type of the aggregate function. </param>
        /// <param name="holder"> An expression to compute values with for the aggregate.</param>
        public static Aggregate FactoryBucketType(string funcName, Type compType, ExpressionHolder holder = null)
        {
            if (funcName == "count" && compType == typeof(int)) return new BucketCount(holder);
            else if (funcName == "max" && compType == typeof(int)) return new IntBucketMax(holder);
            else if (funcName == "max" && compType == typeof(string)) return new StrBucketMax(holder);
            else if (funcName == "min" && compType == typeof(int)) return new IntBucketMin(holder);
            else if (funcName == "min" && compType == typeof(string)) return new StrBucketMin(holder);
            else if (funcName == "avg" && compType == typeof(int)) return new IntBucketAvg(holder);
            else if (funcName == "sum" && compType == typeof(int)) return new IntBucketSum(holder);
            else throw new ArgumentException($"Aggregate factory, trying to create a non existent bucket bound aggregate. {funcName}, {compType}");
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

    }
}
