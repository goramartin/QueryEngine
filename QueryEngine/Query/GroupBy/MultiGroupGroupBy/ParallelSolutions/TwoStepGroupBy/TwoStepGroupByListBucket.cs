using System.Collections.Generic;
using System.Collections.Concurrent;

namespace QueryEngine
{
    internal sealed class TwoStepGroupByListBucket : TwoStepGroupBy
    {
        public TwoStepGroupByListBucket(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        #region MixListsBuckets

        /// <summary>
        /// A main work of each thread when grouping.
        /// The values are stored using arrays in the first step (an index corresponding to a group results is placed as a value on a key, the results can be then accessed via the stored index).
        /// In the second step, the values are reinserted into newly created buckets.
        /// For each result row, perform a local grouping with a simple dictionary storing aggs. results in lists.
        /// Afterwards merge the computed groups with the groups in the global dictionary and store the agg. results in buckets.
        /// Notice that the local part is using hash cache with comparers when inserting into the dictionary
        /// and when inserting into the global dictionary, the hash values are stored in the groupDictKey.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        protected override void SingleThreadGroupByWork(object job)
        {
            // Local part with lists
            #region DECL
            var tmpJob = ((GroupByJobMixListsBuckets)job);
            var results = tmpJob.resTable;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            var hasher = tmpJob.hasher;
            var aggResults = tmpJob.aggResults;
            int position;
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.

                if (!groups.TryGetValue(key, out position))
                {
                    position = groups.Count;
                    groups.Add(key, position);
                }
                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].Apply(in row, aggResults[j], position);
            }

            // Global part with buckets
            var globalGroups = tmpJob.globalGroups;
            AggregateBucketResult[] buckets = null;
            AggregateBucketResult[] spareBuckets = AggregateBucketResult.CreateBucketResults(aggregates);
            foreach (var item in groups)
            {
                buckets = globalGroups.GetOrAdd(item.Key, spareBuckets);
                if (spareBuckets != null && object.ReferenceEquals(spareBuckets, buckets))
                    spareBuckets = AggregateBucketResult.CreateBucketResults(aggregates);
                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].MergeThreadSafe(buckets[j], aggResults[j], item.Value);
            }
        }

        #endregion MixListsBuckets

        protected override GroupByJob CreateJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups)
        {
            return new GroupByJobMixListsBuckets(hasher, comparer, aggregates, resTable, start, end, globalGroups);
        }

        private class GroupByJobMixListsBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, int> groups;
            public AggregateListResults[] aggResults;

            public GroupByJobMixListsBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups) : base(hasher, aggregates, resTable, start, end, globalGroups)
            {
                this.groups = new Dictionary<GroupDictKey, int>(comparer);
                this.aggResults = AggregateListResults.CreateListResults(aggregates);
            }
        }
    }
}
