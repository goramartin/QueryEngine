using System.Collections.Concurrent;

namespace QueryEngine
{
    internal sealed class GlobalGroupByBucket : GlobalGroupBy
    {
        public GlobalGroupByBucket(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, add/get a group in/from the global Dictionary and compute the
        /// corresponding aggregate values for the group.
        /// Aggregate functions results are stored using Buckets (an array of value holders stored as a value on a key).
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        protected override void SingleThreadGroupByWork(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobBuckets)job);
            var results = tmpJob.resTable;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            AggregateBucketResult[] buckets = null;
            AggregateBucketResult[] spareBuckets = AggregateBucketResult.CreateBucketResults(aggregates);
            TableResults.RowProxy row;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                buckets = groups.GetOrAdd(i, spareBuckets);
                // If the spare part was inserted, create a brand-new in advance.
                if (spareBuckets != null && object.ReferenceEquals(spareBuckets, buckets))
                    spareBuckets = AggregateBucketResult.CreateBucketResults(aggregates);
                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].ApplyThreadSafe(in row, buckets[j]);
            }
        }

        protected override GroupByJob[] CreateSpecJobs(GroupByJob[] jobs, RowEqualityComparerInt equalityComparer, ITableResults resTable, int current, int addition)
        {
            var concurrentDictBuckets = new ConcurrentDictionary<int, AggregateBucketResult[]>(equalityComparer);
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJobBuckets(concurrentDictBuckets, this.aggregates, resTable, current, current + addition);
                current += addition;
            }
            jobs[jobs.Length - 1] = new GroupByJobBuckets(concurrentDictBuckets, this.aggregates, resTable, current, resTable.NumberOfMatchedElements);
            return jobs;
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public ConcurrentDictionary<int, AggregateBucketResult[]> groups;

            public GroupByJobBuckets(ConcurrentDictionary<int, AggregateBucketResult[]> groups, Aggregate[] aggregates, ITableResults resTable, int start, int end) : base(aggregates, resTable, start, end)
            {
                this.groups = groups;
            }
        }

        protected override GroupByResults CreateGroupByResults(GroupByJob job)
        {
            var tmpJob = (GroupByJobBuckets)job;
            return new ConDictIntBucket(tmpJob.groups, tmpJob.resTable);
        }
    }
}
