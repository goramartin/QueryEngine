using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Class representing a streamed group by if a clause group by is set.
    /// The computation does not store results of the matcher in any way, instead it stores only the 
    /// group keys and aggregate func. results in one entry in the dictionary using AggregateBucketResult[].
    /// Note that the array contains directly the values and not row proxies.
    /// The first n buckets are used as a key inside the dictionary, while the rest are used as value holders for the computed
    /// aggregate function.
    /// </summary>
    internal class GlobalGroupStreamed : GroupResultProcessor
    {
        private ConcurrentDictionary<AggregateBucketResult[], AggregateBucketResult[]> parGroups = null;
        private Dictionary<AggregateBucketResult[], AggregateBucketResult[]> stGroups = null;
        private BucketsKeyValueFactory[] matcherBucketFactories;

        public GlobalGroupStreamed(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount, int[] usedVars) : base(expressionInfo, executionHelper, columnCount, usedVars)
        {
            this.matcherBucketFactories = new BucketsKeyValueFactory[this.executionHelper.ThreadCount];
            for (int i = 0; i < this.executionHelper.ThreadCount; i++)
                this.matcherBucketFactories[i] = new BucketsKeyValueFactory(this.aggregates, this.hashes);
            
            var comparer = new RowEqualityComparerAggregateBucketResult(this.hashes.Length, this.hashes);
            if (this.executionHelper.InParallel) this.parGroups = new ConcurrentDictionary<AggregateBucketResult[], AggregateBucketResult[]>(comparer);
            else this.stGroups = new Dictionary<AggregateBucketResult[], AggregateBucketResult[]>(comparer);
        }

        public override void Process(int matcherID, Element[] result)
        {
            if (result == null) return;

            var bucketFactory = this.matcherBucketFactories[matcherID];
            var buckets = bucketFactory.Create(result);

            if (this.executionHelper.InParallel) bucketFactory.lastWasInserted = ProcessParallel(result, buckets);
            else bucketFactory.lastWasInserted = ProcessSingleThread(result, buckets);
        }

        /// <summary>
        /// Tries to add it into a dictionary.
        /// If it was added, the returnedBuckets contain the passed buckets into the function.
        /// Otherwise the variable contains buckets that has been added before.
        /// </summary>
        private bool ProcessParallel(Element[] result, AggregateBucketResult[] buckets)
        {
            var returnedBuckets = this.parGroups.GetOrAdd(buckets, buckets);
            for (int i = 0; i < this.aggregates.Length; i++)
                this.aggregates[i].ApplyThreadSafe(result, returnedBuckets[i+this.hashes.Length]);
            
            if (object.ReferenceEquals(returnedBuckets, buckets)) return true;
            else return false;
        }

        private bool ProcessSingleThread(Element[] result, AggregateBucketResult[] buckets)
        {
            bool added = false;
            if (this.stGroups.TryGetValue(buckets, out AggregateBucketResult[] returnedBuckets))
                buckets = returnedBuckets;
            else
            {
                this.stGroups.Add(buckets, buckets);
                added = true;
            }

            for (int i = 0; i < this.aggregates.Length; i++)
                this.aggregates[i].Apply(result, buckets[i + this.hashes.Length]);

            return added;
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            resTable = new TableResults();
            if (this.executionHelper.InParallel)
                groupByResults = new ConDictStreamedBucket(this.parGroups, resTable);
            else groupByResults = new DictStreamedBucket(this.stGroups, resTable);
        }
    }
}
