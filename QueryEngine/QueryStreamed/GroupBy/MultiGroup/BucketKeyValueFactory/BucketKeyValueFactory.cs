using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal class BucketsKeyValueFactory
    {
        public AggregateBucketResult[] bucketsKeyValue;
        public bool lastWasInserted = true;
        private List<Aggregate> aggregates;
        private List<BucketKeyFactory> evaluators;
        private int keysCount;


        public AggregateBucketResult[] Create()
        {





        }



        private abstract class BucketKeyFactory
        {
            abstract public AggregateBucketResult Create(bool lastWasInserted, Element[] result); 

        }

        private class BucketKeyFactory<T> : BucketKeyFactory
        {
            private ExpressionReturnValue<T> expr;
            private AggregateBucketResultStreamed<T> lastCreatedBucket;

            public override AggregateBucketResult Create(bool lastWasInserted, Element[] result)
            {
               




            }
        }
    }
}
