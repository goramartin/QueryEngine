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
    /// The class uses aggretation with buckets or arrays.
    /// Each thread recieves a portion of the results from the result table.
    /// Subsequently, the threads start to aggrgate the results with the help of 
    /// a global ConcurrentDictionary.
    /// </summary>
    internal class GlobalGroup : Grouper
    {
        public GlobalGroup(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        public override AggregateResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out List<ExpressionEqualityComparer> equalityComparers, out List<ExpressionHasher> hashers);
            if (this.InParallel && ((resTable.NumberOfMatchedElements / this.ThreadCount) > 1)) return this.ParallelGroupBy(new RowEqualityComparerInt(resTable, equalityComparers, new RowHasher(hashers), false), resTable);
            else return SingleThreadGroupBy(new RowEqualityComparerInt(resTable, equalityComparers, new RowHasher(hashers), false), resTable);
        }

        /// <summary>
        /// Computes aggregates and groups in parallel.
        /// Each thread receives a portion from the result table and tries to add/get
        /// the each row into the global dictionary and receives a group, subsequently computes aggregates for the group.
        /// </summary>
        private AggregateResults ParallelGroupBy(RowEqualityComparerInt equalityComparer, ITableResults results)
        {
            var jobs = CreateJobs(equalityComparer, results);
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

            return null;
        }
        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// The addition must always be > 0.
        /// Each job will receive a range from result table, aggregates and a global place to store groups.
        /// Note that everything is shared. Nothing is a hard copy. The equalityComparer, the aggregates, results and the concurernt dictionary, semaphore, are shared
        /// among all threads. They hold no state.
        /// </summary>
        private GroupByJob[] CreateJobs(RowEqualityComparerInt equalityComparer, ITableResults results)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");

            if (this.BucketStorage) return CreateJobsBuckets(jobs, equalityComparer, results, current, addition);
            else return CreateJobsArrays(jobs, equalityComparer, results, current, addition);
        }

        private GroupByJob[] CreateJobsArrays(GroupByJob[] jobs, RowEqualityComparerInt equalityComparer, ITableResults results, int current, int addition)
        {
            var aggResults = AggregateArrayResults.CreateArrayResults(this.aggregates);
            var concurrentDictArrays = new ConcurrentDictionary<int, int>(equalityComparer);
            int capture = 0;
            Func<int, int> positionFactory = (int x) => { return Interlocked.Increment(ref capture); };
            Semaphore semaphore = new Semaphore(0, this.ThreadCount);

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJobArrays(concurrentDictArrays, this.aggregates, results, current, current + addition, aggResults, positionFactory, semaphore, jobs.Length);
                current += addition;
            }
            jobs[jobs.Length - 1] = new GroupByJobArrays(concurrentDictArrays, this.aggregates, results, current, current + addition, aggResults, positionFactory, semaphore, jobs.Length);
            return jobs;
        }

        private GroupByJob[] CreateJobsBuckets(GroupByJob[] jobs, RowEqualityComparerInt equalityComparer, ITableResults results, int current, int addition)
        {
            Func<int, AggregateBucketResult[]> bucketFactory = (int x) => { return AggregateBucketResult.CreateBucketResults(this.aggregates); };
            var concurrentDictBuckets = new ConcurrentDictionary<int, AggregateBucketResult[]>(equalityComparer);
            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJobBuckets(concurrentDictBuckets, this.aggregates, results, current, current + addition, bucketFactory);
                current += addition;
            }
            jobs[jobs.Length - 1] = new GroupByJobBuckets(concurrentDictBuckets, this.aggregates, results, current, current + addition, bucketFactory);
            return jobs;
        }

        /// <summary>
        /// Computes single threadedly aggregates.
        /// </summary>
        private AggregateResults SingleThreadGroupBy(RowEqualityComparerInt equalityComparer, ITableResults results)
        {
            object tmpJob;
            if (this.BucketStorage)
            {
                Func<int, AggregateBucketResult[]> bucketFactory = (int x) => { return AggregateBucketResult.CreateBucketResults(this.aggregates); };
                tmpJob = new GroupByJobBuckets(new ConcurrentDictionary<int, AggregateBucketResult[]>(equalityComparer), this.aggregates, results, 0, results.NumberOfMatchedElements, bucketFactory);
            }
            else
            {
                var aggResults = AggregateArrayResults.CreateArrayResults(this.aggregates);
                int capture = 0;
                Func<int, int> positionFactory = (int x) => { return Interlocked.Increment(ref capture); };
                Semaphore semaphore = new Semaphore(0, this.ThreadCount);
                tmpJob = new GroupByJobArrays(new ConcurrentDictionary<int, int>(equalityComparer), this.aggregates, results, 0, results.NumberOfMatchedElements, aggResults, positionFactory, semaphore, ThreadCount);
            }
            SingleThreadGroupByWork(tmpJob, this.BucketStorage);
            return null;
        }

        public static void SingleThreadGroupByWork(object job, bool bucketStorage)
        {
            if (bucketStorage) SingleThreadGroupByWorkWithBuckets(job);
            else SingleThreadGroupByWorkWithArrays(job);
        }

        #region WithArrays

        /// <summary>
        /// Thread safe grouping using arrays.
        /// Firstly, a position is inserted into the dictionary, note that the constructor
        /// runs outside of the synchronization, so it can happen that other thread inserted an element
        /// in the meanwhile.
        /// Then, if the position does exceed the array lenght, it acquires entire semaphore and doubles the arrays.
        /// Otherwise, it acquires only one part of the semaphore, updates the aggregates on the given position and releases the semaphore's part.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        private static void SingleThreadGroupByWorkWithArrays(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobArrays)job);
            var results = tmpJob.results;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            var semaphore = tmpJob.semaphore;
            Func<int,int> positionFactory = tmpJob.positionFactory;
            var aggResults = tmpJob.aggResults;
            int position;
            TableResults.RowProxy row;
            int threadCount = tmpJob.threadCount;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                position = groups.GetOrAdd(i, positionFactory);

                if (aggregates.Count == 0) continue;
                else
                {
                    if (aggResults[0].ArraySize() <= position)
                    {
                        lock(groups) {
                            if (aggResults[0].ArraySize() <= position) {
                                // Acquire entire semaphore.
                                for (int enters = 0; enters < threadCount; enters++) semaphore.WaitOne(); 
                                // Double the array sizes
                                for (int j = 0; j < aggResults.Count; j++) aggResults[j].DoubleSize(position);
                            }
                        }
                    }

                    semaphore.WaitOne();

                    for (int j = 0; j < aggregates.Count; j++)
                        aggregates[j].ApplyThreadSafe(in row, aggResults[j], position);

                    semaphore.Release();
                }
            }
        }

        #endregion WithArrays

        #region WithBuckets

        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, add/get a group in/from the global dictionary and compute the
        /// corresponding aggregate values for the group.
        /// Computation results are stored using buckets.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        private static void SingleThreadGroupByWorkWithBuckets(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobBuckets)job);
            var results = tmpJob.results;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            AggregateBucketResult[] buckets = null;
            TableResults.RowProxy row;
            var bucketFactory = tmpJob.bucketFactory;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                buckets = groups.GetOrAdd(i, bucketFactory);
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].ApplyThreadSafe(in row, buckets[j]);
            }
        }

        #endregion WithBuckets

        #region Jobs

        private abstract class GroupByJob
        {
            public List<Aggregate> aggregates;
            public ITableResults results;
            public int start;
            public int end;
            public GroupByJob(List<Aggregate> aggregates, ITableResults results, int start, int end)
            {
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
            }
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public ConcurrentDictionary<int, AggregateBucketResult[]> groups;
            public Func<int, AggregateBucketResult[]> bucketFactory;

            public GroupByJobBuckets(ConcurrentDictionary<int, AggregateBucketResult[]> groups, List<Aggregate> aggregates, ITableResults results, int start, int end, Func<int, AggregateBucketResult[]> bucketFactory) : base(aggregates, results, start, end)
            {
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
                this.groups = groups;
                this.bucketFactory = bucketFactory;
            }
        }

        private class GroupByJobArrays : GroupByJob
        {
            public ConcurrentDictionary<int, int> groups;
            public List<AggregateArrayResults> aggResults;
            public Func<int, int> positionFactory;
            public Semaphore semaphore;
            public int threadCount;

            public GroupByJobArrays(ConcurrentDictionary<int, int> groups, List<Aggregate> aggregates, ITableResults results, int start, int end, List<AggregateArrayResults> aggResults, Func<int, int> positionFactory, Semaphore semaphore, int threadCount) : base(aggregates, results, start, end)
            {
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
                this.groups = groups;
                this.aggResults = aggResults;
                this.positionFactory = positionFactory;
                this.semaphore = semaphore;
                this.threadCount = threadCount;
            }
        }

        #endregion Jobs
    }
     
}
