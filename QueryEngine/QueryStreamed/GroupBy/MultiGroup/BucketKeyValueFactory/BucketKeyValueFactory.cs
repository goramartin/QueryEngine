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
        private List<Aggregate> aggregates;
        private List<ExpressionEvaluator> evaluators;








        private class BucketKeyFactory
        {
            abstract public AggregateBucketResult Create(); 

        }

        private class BucketKeyFactory<T> : BucketKeyFactory
        {

        }
    }
}
