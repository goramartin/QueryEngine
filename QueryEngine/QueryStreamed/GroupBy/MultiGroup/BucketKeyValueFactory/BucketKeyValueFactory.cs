using System;

namespace QueryEngine
{
    /// <summary>
    /// Class servers as a creator of buckets that are inserted into a dictionary during 
    /// streamed version of the group by and the values are directly stored in the key/values, and not as row proxies.
    /// The class creates an array of buckets where the first n buckets are used as keys in the dictionary while
    /// the rest are used as values holders for the computed aggregate values.
    /// </summary>
    internal class BucketsKeyValueFactory
    {
        public bool lastWasInserted = true;
        private AggregateBucketResult[] lastBucketsKeyValue;
        private Aggregate[] aggregates;
        private BucketKeyFactory[] factories;
        private int keysCount;

        public BucketsKeyValueFactory(Aggregate[] aggregates, ExpressionHolder[] hashes)
        {
            this.aggregates = aggregates;
            this.keysCount = hashes.Length;
            this.factories = new BucketKeyFactory[hashes.Length];
            for (int i = 0; i < hashes.Length; i++)
                this.factories[i] = BucketKeyFactory.Factory(hashes[i]);
        }


        /// <summary>
        /// Creates a new array of buckets that is used as key/value into a dictionary inside the streamed version
        /// of group by.
        /// If the last array was inserted into the dictionary, the function inits a brand-new one.
        /// Otherwise it only actualises internal values of the last created one.
        /// </summary>
        public AggregateBucketResult[] Create(Element[] result)
        {
            if (this.lastWasInserted)
            {
                this.lastBucketsKeyValue = new AggregateBucketResult[this.keysCount + this.aggregates.Length];
                // Init the aggregation funcs. storages
                for (int i = this.keysCount; i < this.keysCount + this.aggregates.Length; i++)
                {
                    var agg = this.aggregates[i - this.keysCount];
                    this.lastBucketsKeyValue[i] = AggregateBucketResult.Factory(agg.GetAggregateReturnType(), agg.GetFuncName());
                }
            }

            for (int i = 0; i < keysCount; i++)
                this.lastBucketsKeyValue[i] = factories[i].Create(this.lastWasInserted, result);
            return this.lastBucketsKeyValue;
        }



        private abstract class BucketKeyFactory
        {
            abstract public AggregateBucketResult Create(bool lastWasInserted, Element[] result); 

            public static BucketKeyFactory Factory(ExpressionHolder expressionHolder)
            {
                if (expressionHolder.ExpressionType == typeof(int)) return new BucketKeyFactory<int>(expressionHolder);
                else if (expressionHolder.ExpressionType == typeof(string)) return new BucketKeyFactory<string>(expressionHolder);
                else throw new ArgumentException($"Bucket key factory, unknown type of bucket factory. Type = {expressionHolder.ExpressionType}.");
            }
        }

        private class BucketKeyFactory<T> : BucketKeyFactory
        {
            private ExpressionReturnValue<T> expr;
            private AggregateBucketResultStreamed<T> lastCreatedBucket = null;

            public BucketKeyFactory(ExpressionHolder holder)
            {
                this.expr = (ExpressionReturnValue<T>)holder.Expr;
            }

            /// <summary>
            /// If the last created bucket was inserted into the dictionary, create a new one and init its values.
            /// Otherwise the bucket was not used, thus only reset its internal values and 
            /// try to init those with the new given result.
            /// This is done in order to recycle uninserted buckets.
            /// </summary>
            public override AggregateBucketResult Create(bool lastWasInserted, Element[] result)
            {
                if (lastWasInserted) this.lastCreatedBucket = new AggregateBucketResultStreamed<T>();
                else 
                {   
                    // Reset internal values.
                    this.lastCreatedBucket.aggResult = default;
                    this.lastCreatedBucket.isSet = false;
                }

                // Try init values with the provided result.
                if (this.expr.TryEvaluate(result, out T returnValue))
                {
                    this.lastCreatedBucket.aggResult = returnValue;
                    this.lastCreatedBucket.isSet = true;
                }

                return this.lastCreatedBucket;
            }
        }
    }
}
