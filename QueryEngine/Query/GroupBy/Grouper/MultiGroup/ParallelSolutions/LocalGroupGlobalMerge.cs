﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a grouping algorithm.
    /// The class should be used only as the parallel solution and not with thread count set to 1.
    /// The algorithm is composed of a local group by and a global merge (two way group by)
    /// The algorithm uses only aggregate bucket storages or a mix of lists and buckets.
    /// Firstly, each thread receives a range from the results table, hasher and comparer and computes
    /// localy its groups, afterwards, they insert the groups into a global dictionary and merge their results.
    /// </summary>
    internal class LocalGroupGlobalMerge : Grouper
    {
        public LocalGroupGlobalMerge(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        public override GroupByResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers);
            return ParallelGroupBy(resTable, comparers, hashers);
        }

        /// <summary>
        /// Note that the received hashers and equality comparers have already set their internal cache to each other.
        /// </summary>
        private GroupByResults ParallelGroupBy(ITableResults resTable, ExpressionComparer[] comparers, ExpressionHasher[] hashers)
        {
            GroupByJob[] jobs = CreateJobs(resTable, this.aggregates, comparers, hashers);
            var tasks = new Task[this.ThreadCount - 1];

            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = jobs[i];
                tasks[i] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp, this.BucketStorage));
            }

            // The main thread works with the last job in the array.
            SingleThreadGroupByWork(jobs[jobs.Length - 1], this.BucketStorage);
            Task.WaitAll(tasks);
            // No merge needed.

            return CreateGroupByResults(jobs[0]);
        }

        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// The addition must always be > 0.
        /// Each job will receive a range from result table, hasher, comparer and aggregates.
        /// Note that they are all copies, because they contain a private stete (hasher contains reference to the equality comparers to enable caching when computing the hash, aggregates
        /// contain references to storage arrays to avoid casting in a tight loop).
        /// The comparers and hashers build in the constructor of this class are given to the last job, just like the aggregates passed to the construtor.
        /// The global dictionary recieves a comparer that has no internal comparers set to some hasher.
        /// </summary>
        private GroupByJob[] CreateJobs(ITableResults resTable, Aggregate[] aggs, ExpressionComparer[] comparers, ExpressionHasher[] hashers)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = resTable.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");

            var lastComp = RowEqualityComparerGroupKey.Factory(resTable, comparers, true);
            var lastHasher = new RowHasher(hashers);
            lastHasher.SetCache(lastComp.comparers);

            // Global merge dictionary
            // It needs only comparator that has no comparers set as a cache to some hasher.
            var globalGroups = new ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]>(lastComp.Clone(cacheResults: false));
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                var tmpComp = lastComp.Clone(cacheResults: true);
                var tmpHash = lastHasher.Clone();
                tmpHash.SetCache(tmpComp.comparers);
                if (this.BucketStorage) jobs[i] = new GroupByJobBuckets(tmpHash, tmpComp, aggs, resTable, current, current + addition, globalGroups);
                else jobs[i] = new GroupByJobMixListsBuckets(tmpHash, tmpComp, aggs, resTable, current, current + addition, globalGroups);
                current += addition;
            }
            if (this.BucketStorage) jobs[jobs.Length - 1] = new GroupByJobBuckets(lastHasher, lastComp, aggs, resTable, current, resTable.NumberOfMatchedElements, globalGroups);
            else jobs[jobs.Length - 1] = new GroupByJobMixListsBuckets(lastHasher, lastComp, aggs, resTable, current, resTable.NumberOfMatchedElements, globalGroups);
            return jobs;
        }

        private static void SingleThreadGroupByWork(object job, bool useBucketStorage)
        {
            if (useBucketStorage) SingleThreadGroupByWorkBuckets(job);
            else SingleThreadGroupByWorkMixListsBuckets(job);
        }

        #region Buckets
        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, perform a local grouping with a simple dictionary.
        /// Afterwards merge the computed groups with the groups in the global dictionary.
        /// Notice that the local part is using hash cache with comparers when inserting into the dictionary
        /// and when inserting into the global dictionary, the hash values are stored in the groupDictKey.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        private static void SingleThreadGroupByWorkBuckets(object job)
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

        #region MixListsBuckets

        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, perform a local grouping with a simple dictionary storing aggs. results in lists.
        /// Afterwards merge the computed groups with the groups in the global dictionary and store the agg. results in buckets.
        /// Notice that the local part is using hash cache with comparers when inserting into the dictionary
        /// and when inserting into the global dictionary, the hash values are stored in the groupDictKey.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        private static void SingleThreadGroupByWorkMixListsBuckets(object job)
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

        #region Jobs

        private abstract class GroupByJob
        {
            public RowHasher hasher;
            public Aggregate[] aggregates;
            public ITableResults resTable;
            public int start;
            public int end;
            public ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups;

            public GroupByJob(RowHasher hasher, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups)
            {
                this.hasher = hasher;
                this.aggregates = aggregates;
                this.resTable = resTable;
                this.start = start;
                this.end = end;
                this.globalGroups = globalGroups;
            }
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, AggregateBucketResult[]> groups;

            public GroupByJobBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups): base(hasher, aggregates, resTable, start, end, globalGroups)
            {
                this.groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>(comparer);
            }
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

        #endregion Jobs

        private GroupByResults CreateGroupByResults(GroupByJob job)
        {
            return new ConDictGroupByResultsBucket(job.globalGroups, job.resTable);
        }
    }
}
