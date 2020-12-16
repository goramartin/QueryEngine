using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a grouping algorithm.
    /// The algorithm is composed of a local group by and a global merge (two way group by)
    /// The algorithm uses only aggregate bucket storages or a mix of lists and buckets.
    /// Firstly, each thread receives a range from the results table, hasher and comparer and computes
    /// localy its groups, afterwards, they insert the groups into a global dictionary and merge their results.
    /// </summary>
    internal class LocalGroupGlobalMerge : Grouper
    {
        public LocalGroupGlobalMerge(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        public override GroupByResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out List<ExpressionEqualityComparer> equalityComparers, out List<ExpressionHasher> hashers);
            return ParallelGroupBy(resTable, equalityComparers, hashers);
        }

        /// <summary>
        /// Note that the received hashers and equality comparers have already set their internal cache to each other.
        /// </summary>
        private GroupByResults ParallelGroupBy(ITableResults resTable, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            GroupByJob[] jobs = CreateJobs(resTable, this.aggregates, equalityComparers, hashers);
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
        private GroupByJob[] CreateJobs(ITableResults results, List<Aggregate> aggs, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");

            var lastComp = new RowEqualityComparerGroupKey(results, equalityComparers);
            var lastHasher = new RowHasher(hashers);
            lastComp.SetCache(lastHasher);
            lastHasher.SetCache(lastComp.Comparers);

            // Global merge dictionary
            // It needs only comparator that has no comparers set as a cache to some hasher.
            var globalGroups = new ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]>(lastComp.Clone());
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                var tmpComp = lastComp.Clone();
                var tmpHash = lastHasher.Clone();
                tmpComp.SetCache(tmpHash);
                tmpHash.SetCache(tmpComp.Comparers);
                if (this.BucketStorage) jobs[i] = new GroupByJobBuckets(tmpHash, tmpComp, aggs, results, current, current + addition, globalGroups);
                else jobs[i] = new GroupByJobMixListsBuckets(tmpHash, tmpComp, aggs, results, current, current + addition, globalGroups);
                current += addition;
            }
            if (this.BucketStorage) jobs[jobs.Length - 1] = new GroupByJobBuckets(lastHasher, lastComp, aggs, results, current, results.NumberOfMatchedElements, globalGroups);
            else jobs[jobs.Length - 1] = new GroupByJobMixListsBuckets(lastHasher, lastComp, aggs, results, current, results.NumberOfMatchedElements, globalGroups);
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
            var results = tmpJob.results;
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
                for (int j = 0; j < aggregates.Count; j++)
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
                if (!Object.ReferenceEquals(buckets, item.Value))
                {
                    for (int j = 0; j < aggregates.Count; j++)
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
            var results = tmpJob.results;
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
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in row, aggResults[j], position);
            }

            // Global part with buckets
            var globalGroups = tmpJob.globalGroups;
            AggregateBucketResult[] buckets = null;
            AggregateBucketResult[] spareBuckets = AggregateBucketResult.CreateBucketResults(aggregates);
            foreach (var item in groups)
            {
                buckets = globalGroups.GetOrAdd(item.Key, spareBuckets);
                if (object.ReferenceEquals(spareBuckets, buckets))
                    spareBuckets = AggregateBucketResult.CreateBucketResults(aggregates);
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].MergeThreadSafe(buckets[j], aggResults[j], item.Value);
            }
        }

        #endregion MixListsBuckets

        #region Jobs

        private abstract class GroupByJob
        {
            public RowHasher hasher;
            public List<Aggregate> aggregates;
            public ITableResults results;
            public int start;
            public int end;
            public ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups;

            public GroupByJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, List<Aggregate> aggregates, ITableResults results, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups)
            {
                this.hasher = hasher;
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
                this.globalGroups = globalGroups;
            }
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, AggregateBucketResult[]> groups;

            public GroupByJobBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, List<Aggregate> aggregates, ITableResults results, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups): base(hasher, comparer, aggregates, results, start, end, globalGroups)
            {
                this.groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>((IEqualityComparer<GroupDictKey>)comparer);
            }
        }

        private class GroupByJobMixListsBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, int> groups;
            public List<AggregateListResults> aggResults;

            public GroupByJobMixListsBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, List<Aggregate> aggregates, ITableResults results, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups) : base(hasher, comparer, aggregates, results, start, end, globalGroups)
            {
                this.groups = new Dictionary<GroupDictKey, int>((IEqualityComparer<GroupDictKey>)comparer);
                this.aggResults = AggregateListResults.CreateArrayResults(aggregates);
            }
        }

        #endregion Jobs

        private GroupByResults CreateGroupByResults(GroupByJob job)
        {
            return new ConDictGroupByResultsBucket(job.globalGroups, job.results);
        }
    }
}
