using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class LocalGroupByLocalTwoWayMergeBucket : LocalGroupByLocalTwoWayMerge
    {
        public LocalGroupByLocalTwoWayMergeBucket(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        #region WithBuckets

        /// <summary>
        /// Main work of a thread when merging with another threads groups.
        /// Merge only if there is already the given group, otherwise the buckets
        /// are inserted into the jobs1's dictionary.
        /// </summary>
        /// <param name="job1"> A place to merge into. </param>
        /// <param name="job2"> A place to merge from. </param>
        protected override void SingleThreadMergeWork(object job1, object job2)
        {
            #region DECL
            var groups1 = ((GroupByJobBuckets)job1).groups;
            var groups2 = ((GroupByJobBuckets)job2).groups;
            var aggregates = ((GroupByJobBuckets)job1).aggregates;
            AggregateBucketResult[] buckets;
            #endregion DECL

            foreach (var item in groups2)
            {
                if (!groups1.TryGetValue(item.Key, out buckets))
                {
                    groups1.Add(item.Key, item.Value);
                    // No need to merge the results.
                    continue;
                }
                // It merges the results only if the group was already in the dictionary.
                for (int i = 0; i < aggregates.Length; i++)
                    aggregates[i].Merge(buckets[i], item.Value[i]);
            }
        }

        /// <summary>
        /// Main work of a thread when grouping.
        /// For each result row.
        /// Try to add it to the dictionary and apply aggregate functions with the rows.
        /// Note that when the hash is computed. The comparer cache is set.
        /// So when the insertion happens, it does not have to compute the values for comparison.
        /// </summary>
        protected override void SingleThreadGroupByWork(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobBuckets)job);
            var hasher = tmpJob.hasher;
            var aggregates = tmpJob.aggregates;
            var results = tmpJob.resTable;
            var groups = tmpJob.groups;
            AggregateBucketResult[] buckets;
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
        }

        #endregion WithBuckets

        protected override GroupByJob CreateJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end)
        {
           return new GroupByJobBuckets(hasher, comparer, aggregates, resTable, start, end);
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, AggregateBucketResult[]> groups;

            public GroupByJobBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end) : base(hasher, comparer, aggregates, resTable, start, end)
            {
                this.groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>(comparer);
            }
        }

        protected override GroupByResults CreateGroupByResults(GroupByJob job)
        {
            var tmp = (GroupByJobBuckets)job;
            return new DictGroupDictKeyBucket(tmp.groups, tmp.resTable);
        }
    }
}
