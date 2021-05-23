using System.Collections.Generic;
using System.Collections.Concurrent;

namespace QueryEngine
{
    internal sealed class TwoStepGroupByBucket : TwoStepGroupBy
    {
        public TwoStepGroupByBucket(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        #region Buckets

        /// <summary>
        /// A main work of each thread when grouping.
        /// The values are stored using arrays (an index corresponding to a group results is placed as a value on a key, the results can be then accessed via the stored index).
        /// For each result row, perform a local grouping with a simple Dictionary.
        /// Afterwards merge the computed groups with the groups in the global Dictionary.
        /// Notice that the local part is using hash cache with comparers when inserting into the Dictionary
        /// and when inserting into the global Dictionary, the hash values are stored in the groupDictKey.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        protected override void SingleThreadGroupByWork(object job)
        {
            // Local part 
            #region DECL
            var tmpJob = ((GroupByJobBuckets)job);
            var results = tmpJob.resTable;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            var hasher = tmpJob.hasher;
            AggregateBucketResult[] buckets = null;
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.
                if (!groups.TryGetValue(key, out buckets))
                {
                    buckets = AggregateBucketResult.CreateBucketResults(aggregates);
                    groups.Add(key, buckets);
                }
                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].Apply(in row, buckets[j]);
            }

            // Global part
            var globalGroups = tmpJob.globalGroups;

            foreach (var item in groups)
            {
                buckets = globalGroups.GetOrAdd(item.Key, item.Value);
                // Note that the returned value can be the same as given in arguments. 
                // That means that it inserted the given group.
                // If it did not, merge its results with the returned one.
                if (item.Value != null && !object.ReferenceEquals(buckets, item.Value))
                {
                    for (int j = 0; j < aggregates.Length; j++)
                        aggregates[j].MergeThreadSafe(buckets[j], item.Value[j]);
                }
            }
        }

        #endregion Buckets

        protected override GroupByJob CreateJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups)
        {
            return new GroupByJobBuckets(hasher, comparer, aggregates, resTable, start, end, globalGroups);
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, AggregateBucketResult[]> groups;

            public GroupByJobBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups) : base(hasher, aggregates, resTable, start, end, globalGroups)
            {
                this.groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>(comparer);
            }
        }
    }
}
