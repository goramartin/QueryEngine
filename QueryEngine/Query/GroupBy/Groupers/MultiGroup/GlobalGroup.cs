using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// A class represents a multi group grouping algorithm.
    /// The class uses aggretation with buckets storages.
    /// Each thread recieves a portion of the results from the result table.
    /// Subsequently, the threads start to aggrgate the results with the help of 
    /// a global ConcurrentDictionary.
    /// </summary>
    internal class GlobalMerge : Grouper
    {
        private List<AggregateBucket> bucketAggregates = null;
        public GlobalMerge(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        { }

        public override AggregateResults Group(ITableResults resTable)
        {
            // Create bucket aggregates
            this.bucketAggregates = new List<AggregateBucket>();
            for (int i = 0; i < this.aggregates.Count; i++)
                this.bucketAggregates.Add((AggregateBucket)Aggregate.FactoryBucketType(this.aggregates[i]));

            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            var equalityComparers = new List<ExpressionEqualityComparer>();
            var hashers = new List<ExpressionHasher>();
            for (int i = 0; i < hashes.Count; i++)
            {
                equalityComparers.Add(ExpressionEqualityComparer.Factory(hashes[i], hashes[i].ExpressionType));
                hashers.Add(ExpressionHasher.Factory(hashes[i], hashes[i].ExpressionType, null));
            }

            if (this.InParallel && ((resTable.NumberOfMatchedElements / this.ThreadCount) > 1)) return this.ParallelGroupBy(new RowEqualityComparerWithHash(resTable, equalityComparers, new RowHasher(hashers), false), resTable);
            else return SingleThreadGroupBy(new RowEqualityComparerWithHash(resTable, equalityComparers, new RowHasher(hashers), false), resTable);
        }


        /// <summary>
        /// Computes aggregates and groups in parallel.
        /// Each thread receives a portion from the result table and tries to add/get
        /// the each row into the global dictionary and receives a group, subsequently computes aggregates for the group.
        /// </summary>
        private AggregateResults ParallelGroupBy(RowEqualityComparerWithHash equalityComparer, ITableResults results)
        {
            var jobs = CreateJobs(equalityComparer, results);
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
        /// Computes single threadedly aggregates.
        /// </summary>
        private AggregateResults SingleThreadGroupBy(RowEqualityComparerWithHash equalityComparer, ITableResults results)
        {
            Func<int, AggregateBucketResult[]> bucketFactory = (int x) => { return AggregateBucketResult.CreateBucketResults(this.bucketAggregates); };
            var tmpJob = new GroupByJob(new ConcurrentDictionary<int, AggregateBucketResult[]>(equalityComparer), this.bucketAggregates, results, 0, results.NumberOfMatchedElements, bucketFactory);
            SingleThreadGroupByWork(tmpJob);
           
            return null;
        }


        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// The addition must always be > 0.
        /// Each job will receive a range from result table, aggregates and a global place to store groups.
        /// Note that everything is shared. Nothing is a hard copy. The equalityComparer, the bucket aggregates, results and the concurernt dictionary are shared
        /// among all threads. They hold no state.
        /// </summary>
        private GroupByJob[] CreateJobs(RowEqualityComparerWithHash equalityComparer, ITableResults results)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");

            Func<int, AggregateBucketResult[]> bucketFactory = (int x) => { return AggregateBucketResult.CreateBucketResults(this.bucketAggregates); };
            var concurrentDict = new ConcurrentDictionary<int, AggregateBucketResult[]>(equalityComparer);
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJob(concurrentDict,this.bucketAggregates, results, current, current + addition, bucketFactory);
                current += addition;
            }
            jobs[jobs.Length - 1] = new GroupByJob(concurrentDict, this.bucketAggregates, results, current, results.NumberOfMatchedElements, bucketFactory);
            return jobs;
        }


        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, add/get a group in/from the global dictionary and compute the
        /// corresponding aggregate values for the group.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        private static void SingleThreadGroupByWork(object job)
        {
            var tmpJob = ((GroupByJob)job);
            var results = tmpJob.results;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            AggregateBucketResult[] buckets = null;
            TableResults.RowProxy row;
            var bucketFactory = tmpJob.bucketFactory;

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                buckets = groups.GetOrAdd(i, bucketFactory);
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].ApplyThreadSafe(in row, buckets[j]);
            }
        }

        private class GroupByJob
        {
            public List<AggregateBucket> aggregates;
            public ITableResults results;
            public ConcurrentDictionary<int, AggregateBucketResult[]> groups;
            public int start;
            public int end;
            public Func<int, AggregateBucketResult[]> bucketFactory;
            public GroupByJob(ConcurrentDictionary<int, AggregateBucketResult[]> groups, List<AggregateBucket> aggregates, ITableResults results, int start, int end, Func<int, AggregateBucketResult[]> bucketFactory)
            {
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
                this.groups = groups;
                this.bucketFactory = bucketFactory;
            }
        }
    }
     
}
