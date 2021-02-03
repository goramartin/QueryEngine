using System;
using System.Collections.Generic;

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
        /// A number of keys used to group the results.
        /// This is used only in the streamed version of the group by.
        /// Because the specific aggregate values are stored after the key values.
        /// </summary>
        protected int KeyCount { get; }

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
        /// <param name="keyCount"> A key count. </param>
        public AggregateReference(int aggPos, int keyCount, Aggregate agg)
        {
            this.AggrPosition = aggPos;
            this.Aggr = agg;
            this.KeyCount = keyCount;
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

        public override bool TryEvaluate(in TableResults.RowProxy elements, out T returnValue)
        {
            throw new NotImplementedException();
        }

        public override bool TryEvaluate(in Element[] elements, out T returnValue)
        {
            throw new NotImplementedException();
        }

        public override bool TryEvaluate(in GroupByResultsList.GroupProxyList group, out T returnValue)
        {
            returnValue = group.GetValue<T>(this.AggrPosition);
            return true;
        }

        public override bool TryEvaluate(in GroupByResultsBucket.GroupProxyBucket group, out T returnValue)
        {
            returnValue = group.GetValue<T>(this.AggrPosition);
            return true;
        }

        public override bool TryEvaluate(in GroupByResultsArray.GroupProxyArray group, out T returnValue)
        {
            returnValue = group.GetValue<T>(this.AggrPosition);
            return true;
        }

        public override bool TryEvaluate(in AggregateBucketResult[] group, out T returnValue)
        {
            returnValue = AggregateBucketResultStreamedGetValue.GetFinalValue<T>(group[this.KeyCount + this.AggrPosition]);
            return true;
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

        public override int GetHashCode()
        {
            throw new NotImplementedException($"{this.GetType()}, calling not impl. function.");
        }
    }

    internal static class AggregateReferenceFactory 
    {
        /// <summary>
        /// Creates aggregation reference.
        /// </summary>
        /// <param name="type"> Type of aggregation. </param>
        /// <param name="position"> Position of the aggregation in terms of entire query. </param>
        /// <param name="keyCount"> A key count. </param>
        /// <param name="aggr"> Aggregation to be referenced. The purpose is solely for overriding ToString method. </param>
        /// <returns> Expression node that references aggregation. </returns>
        public static ExpressionBase Create(Type type, int position, int keyCount, Aggregate aggr)
        {
            if (type == typeof(int)) return new AggregateReference<int>(position, keyCount, aggr);
            else if (type == typeof(string)) return new AggregateReference<string>(position, keyCount, aggr);
            else if (type == typeof(double)) return new AggregateReference<double>(position, keyCount, aggr);
            else throw new ArgumentException($"AggregateReferenceFactory, trying to create unsupported type = {type}.");
        }
    }
}
