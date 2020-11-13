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
    /// The algorithm uses only aggregate bucket storages.
    /// Firstly, each thread receives a range from the results table, hasher and comparer and computes
    /// localy its groups, afterwards, they insert the groups into a global dictionary and merge their results.
    /// </summary>
    internal class LocalGroupGlobalMergeWithBuckets : Grouper
    {
        public LocalGroupGlobalMergeWithBuckets(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        {
            this.BucketStorage = true;
        }

        public override AggregateResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            var equalityComparers = new List<ExpressionEqualityComparer>();
            var hashers = new List<ExpressionHasher>();
            for (int i = 0; i<hashes.Count; i++)
            {   
                // We also set the cache of hasher to the new comparer.
                equalityComparers.Add(ExpressionEqualityComparer.Factory(hashes[i], hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(hashes[i], hashes[i].ExpressionType, equalityComparers[i]));
            }

            if (this.InParallel && ((resTable.NumberOfMatchedElements / this.ThreadCount) > 1)) return ParallelGroupBy(resTable, equalityComparers, hashers);
            else return SingleThreadGroupBy(resTable, equalityComparers, hashers);
        }

        /// <summary>
        /// Note that the received hashers and equality comparers have already set their internal cache to each other.
        /// </summary>
        private AggregateResults ParallelGroupBy(ITableResults resTable, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            GroupByJob[] jobs = CreateJobs(resTable, this.aggregates, equalityComparers, hashers);
            var tasks = new Task[this.ThreadCount - 1];

            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = jobs[i];
                tasks[i] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp));
            }

            // The main thread works with the last job in the array.
            SingleThreadGroupByWork(jobs[jobs.Length - 1]);
            Task.WaitAll(tasks);
            // No merge needed.

            return null;
        }

        /// <summary>
        /// Note that the received hashers and equality comparers have already set their internal cache to each other.
        /// </summary>
        private AggregateResults SingleThreadGroupBy(ITableResults resTable, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            var tmpComparer = new RowEqualityComparerNoHash(resTable, equalityComparers);
            var tmpGlobalDictionary = new ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]>(tmpComparer);
            var tmpJob = new GroupByJob(new RowHasher(hashers), tmpComparer, this.aggregates, resTable, 0, resTable.NumberOfMatchedElements, tmpGlobalDictionary);
            SingleThreadGroupByWork(tmpJob);
            //return tmp.aggResults;
            return null;
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

            var lastComp = new RowEqualityComparerNoHash(results, equalityComparers);
            var lastHasher = new RowHasher(hashers);

            // Global merge dictionary
            // It needs only comparator that has no comparers set as a cache to some hasher.
            var globalGroups = new ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]>(lastComp.Clone());
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                var tmpComp = lastComp.Clone();
                jobs[i] = new GroupByJob(lastHasher.Clone(tmpComp.Comparers), tmpComp, aggs, results, current, current + addition, globalGroups);
                current += addition;
            }
            jobs[jobs.Length - 1] = new GroupByJob(lastHasher, lastComp, aggs, results, current, results.NumberOfMatchedElements, globalGroups);
            return jobs;
        }

        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, perform a local grouping with a simple dictionary.
        /// Afterwards merge the computed groups with the groups in the global dictionary.
        /// Notice that the local part is using hash cache with comparers when inserting into the dictionary
        /// and when inserting into the global dictionary, the hash values are stored in the groupDictKey.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        private static void SingleThreadGroupByWork(object job)
        {
            // Local part 
            #region DECL
            var tmpJob = ((GroupByJob)job);
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

        private class GroupByJob
        {
            public RowHasher hasher;
            public List<Aggregate> aggregates;
            public ITableResults results;
            public Dictionary<GroupDictKey, AggregateBucketResult[]> groups;
            public int start;
            public int end;
            public ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups;

            public GroupByJob(
                RowHasher hasher,
                RowEqualityComparerNoHash comparer,
                List<Aggregate> aggregates,
                ITableResults results,
                int start,
                int end,
                ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups
                )
            {
                this.hasher = hasher;
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
                this.groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>((IEqualityComparer<GroupDictKey>)comparer);
                this.globalGroups = globalGroups;
            }
        }

    }
}
