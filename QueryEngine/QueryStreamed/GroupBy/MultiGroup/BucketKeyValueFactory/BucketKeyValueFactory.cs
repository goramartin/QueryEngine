using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class BucketsKeyValueFactory
    {
        public AggregateBucketResult[] lastBucketsKeyValue;
        public bool lastWasInserted = true;
        private Aggregate[] aggregates;
        private BucketKeyFactory[] factories;
        private int keysCount;

        public BucketsKeyValueFactory(Aggregate[] aggregates, ExpressionHolder[] hashes)
        {
            this.aggregates = aggregates;
            this.keysCount = hashes.Length;
            this.factories = new BucketKeyFactory[hashes.Length];
            this.keysCount = hashes.Length;
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
                    this.lastBucketsKeyValue[i] = AggregateBucketResult.Factory(this.aggregates[i].GetAggregateReturnType(), this.aggregates[i].GetFuncName());
            }

            for (int i = 0; i < keysCount; i++)
                this.lastBucketsKeyValue[i] = factories[i].Create(this.lastWasInserted, result);
            return this.lastBucketsKeyValue;
        }



        private abstract class BucketKeyFactory
        {
            abstract public AggregateBucketResult Create(bool lastWasInserted, Element[] result); 

            public static BucketKeyFactory Factory(ExpressionHolder holder)
            {
                var exprType = holder.GetExpressionType();
                if (exprType == typeof(int)) return new BucketKeyFactory<int>(holder);
                else if (exprType == typeof(string)) return new BucketKeyFactory<string>(holder);
                else throw new ArgumentException($"Bucket key factory, unknown type of bucket factory. Type = {exprType}.");
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
                    this.lastCreatedBucket.aggResult = default;
                    this.lastCreatedBucket.isSet = false;
                }

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
