using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A class represents a multi group grouping algorithm.
    /// The algorithm works in two steps.
    /// In the first step each threads computes the groups locally.
    /// When a thread is finished it does not wait for other threads to finish but immediately starts merging its results into a global concurrent Dictionary.
    /// The class should be used only as the parallel solution and not with thread count set to 1.
    /// The algorithm is composed of a local group by and a global merge.
    /// The algorithm uses only aggregate buckets or array like storages.
    /// Note that the solution using array like storages uses the arrays in the first step, then it proceeds using buckets during merge.
    /// </summary>
    internal abstract class TwoStepGroupBy : Grouper
    {
        public TwoStepGroupBy(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
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
                tasks[i] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp));
            }

            // The main thread works with the last job in the array.
            SingleThreadGroupByWork(jobs[jobs.Length - 1]);
            Task.WaitAll(tasks);
            // No merge needed.

            return CreateGroupByResults(jobs[0]);
        }

        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// Each job will receive a range from result table, hasher, comparer and aggregates.
        /// Note that they are all copies, because they contain a private stete (hasher contains reference to the equality comparers to enable caching when computing the hash, aggregates
        /// contain references to storage arrays to avoid casting in a tight loop).
        /// The comparers and hashers build in the constructor of this class are given to the last job, just like the aggregates passed to the construtor.
        /// The global Dictionary recieves a comparer that has no internal comparers set to some hasher.
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

            // It needs only comparator that has no comparers set as a cache to some hasher.
            var globalGroups = new ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]>(lastComp.Clone(cacheResults: false));
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                var tmpComp = lastComp.Clone(cacheResults: true);
                var tmpHash = lastHasher.Clone();
                tmpHash.SetCache(tmpComp.comparers);
                jobs[i] = CreateJob(tmpHash, tmpComp, aggs, resTable, current, current + addition, globalGroups);
                current += addition;
            }
            jobs[jobs.Length - 1] = CreateJob(lastHasher, lastComp, aggs, resTable, current, resTable.NumberOfMatchedElements, globalGroups);
            return jobs;
        }

        protected abstract GroupByJob CreateJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> globalGroups);
        protected abstract void SingleThreadGroupByWork(object job);

        #region Jobs

        protected abstract class GroupByJob
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

        #endregion Jobs

        private GroupByResults CreateGroupByResults(GroupByJob job)
        {
            return new ConDictGroupByResultsBucket(job.globalGroups, job.resTable);
        }
    }
}
