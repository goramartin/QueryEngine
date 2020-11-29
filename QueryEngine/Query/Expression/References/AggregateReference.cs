using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class serves as an aggregation reference.
    /// The evaluation of this node is bound to the aggregate holders, thus nothing here is computed.
    /// The evaluate method should be passed an object containg computed aggregates and a position
    /// of the group.
    /// </summary>
    internal class AggregateReference<T> : ExpressionReturnValue<T>
    {
        /// <summary>
        /// Represents a position of the referenced aggregate.
        /// The aggregate position are unique.
        /// </summary>
        protected int AggrPosition { get; }
        /// <summary>
        /// Reference to an aggregate, its purpose is only to properly override ToString().
        /// Otherwise must not be used.
        /// </summary>
        private Aggregate Aggr { get; }

        /// <summary>
        /// Creates aggregate reference.
        /// </summary>
        /// <param name="aggPos"> An aggregation position.</param>
        /// <param name="agg"> An actual aggregation. </param>
        public AggregateReference(int aggPos, Aggregate agg)
        {
            this.AggrPosition = aggPos;
            this.Aggr = agg;
        }


        public override List<int> CollectUsedVars(List<int> vars)
        {
            return vars;
        }

        public override bool ContainsAggregate()
        {
            return true;
        }

        public override Type GetExpressionType()
        {
            return typeof(T);
        }

        public override bool TryEvaluate(in TableResults.RowProxy elements, out T value)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (obj.GetType() != this.GetType()) return false;
            else
            {
                var tmp = (AggregateReference<T>)obj;
                if (this.AggrPosition == tmp.AggrPosition) return true;
                else return false;
            }
        }

        public override string ToString()
        {
            return this.Aggr.ToString();
        }

    }

    internal static class AggregateReferenceFactory 
    {
        /// <summary>
        /// Creates aggregation reference.
        /// </summary>
        /// <param name="type"> Type of aggregation. </param>
        /// <param name="position"> Position of the aggregation in terms of entire query. </param>
        /// <param name="aggr"> Aggregation to be referenced. The purpose is solely for overriding ToString method. </param>
        /// <returns> Expression node that references aggregation. </returns>
        public static ExpressionBase Create(Type type, int position, Aggregate aggr)
        {
            if (type == typeof(int)) return new AggregateReference<int>(position, aggr);
            else if (type == typeof(string)) return new AggregateReference<string>(position, aggr);
            else if (type == typeof(double)) return new AggregateReference<double>(position, aggr);
            else throw new ArgumentException($"AggregateReferenceFactory, trying to create unsupported type = {type}.");
        }
    }
}
