using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// The class represents multi group grouping algorithm.
    /// The algorithm works in two steps.
    /// In the first step each thread computes the groups locally.
    /// When all threads are finished a two way merging is executed.
    /// The class should be used only as the parallel solution and not with thread count set to 1.
    /// The class uses aggregations with array like storage or buckets.
    /// Each thread receives an equality comparer, hasher, aggregates and a range of vertices.
    /// </summary>
    internal abstract class LocalGroupByLocalTwoWayMerge : Grouper
    {
        public LocalGroupByLocalTwoWayMerge(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage) 
        { }

        public override GroupByResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers);
            return ParallelGroupBy(resTable, comparers, hashers);
        }

        private GroupByResults ParallelGroupBy(ITableResults resTable, ExpressionComparer[] comparers, ExpressionHasher[] hashers)
        {
            GroupByJob[] jobs = CreateJobs(resTable, this.aggregates, comparers, hashers);
            ParallelGroupByWork(jobs, 0, ThreadCount);
            return CreateGroupByResults(jobs[0]);
        }

        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// Each job will receive a range from result table, hasher, comparer (cache on) and aggregates.
        /// Note that they are all copies, because they contain a private state (hasher contains reference to the equality comparers to enable caching when computing the hash).
        /// The comparers and hashers build in the constructor of this class are given to the last job, just like the aggregates passed to the construtor.
        /// </summary>
        private GroupByJob[] CreateJobs(ITableResults resTable, Aggregate[] aggs, ExpressionComparer[] comparers, ExpressionHasher[] hashers)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = resTable.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");
             
            // Set their internal cache.
            var lastComp = RowEqualityComparerGroupKey.Factory(resTable, comparers, true);
            var lastHasher = new RowHasher(hashers);
            lastHasher.SetCache(lastComp.comparers);

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                var tmpComp = lastComp.Clone(cacheResults: true); 
                var tmpHash = lastHasher.Clone();
                tmpHash.SetCache(tmpComp.comparers);
                jobs[i] = CreateJob(tmpHash, tmpComp, aggs, resTable, current, current + addition);
                current += addition;
            }
           
            jobs[jobs.Length - 1] = CreateJob(lastHasher, lastComp, aggs, resTable, current, resTable.NumberOfMatchedElements);
            return jobs;
        }


        protected abstract GroupByJob CreateJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end);


        /// <summary>
        /// Creates a binary tree, where on each non leaf level, results from threads are merged. On the 
        /// last level before the leaf level a grouping is started for each thread based on the given range of result table.
        /// And on the same level they are also merged.
        /// That means that the recursion never reaches the leaf level by itself but the leaves represent grouping computations started 
        /// from the last level before leaf level.
        /// The results are merged onto the jobs[0].
        /// </summary>
        /// <param name="jobs"> Jobs for each thread. </param>
        /// <param name="start"> Starting index of a jobs to finish. </param>
        /// <param name="end"> End index of a jobs to finish. </param>
        /// <param name="bucketStorage"> Whether to compute with bucket method versions. </param>
        private void ParallelGroupByWork(GroupByJob[] jobs, int start, int end)
        {
            if (end - start > 3)
            {
                // compute middle of the range
                int middle = ((end - start) / 2) + start;
                if (middle % 2 == 1) middle--;

                Task task = Task.Factory.StartNew(() => ParallelGroupByWork(jobs, middle, end));
                // Current thread work.
                ParallelGroupByWork(jobs, start, middle);

                // Wait for the other task to finish and start merging its results with yours.
                task.Wait();
                SingleThreadMergeWork(jobs[start], jobs[middle]);
                jobs[middle] = null;
            }
            else
            {   // One level before leaf level.
                Task[] tasks = new Task[end - start - 1];
                int taskIndex = 0;
               
                // Start the grouping computations.
                for (int i = start + 1; i < end; i++)
                {
                    var tmp = jobs[i];
                    tasks[taskIndex] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp));
                    taskIndex++;
                }
                SingleThreadGroupByWork(jobs[start]);
                Task.WaitAll(tasks);
                
                // Merge the results from the leaf level.
                for (int i = start + 1; i < end; i++)
                {
                   SingleThreadMergeWork(jobs[start], jobs[i]);
                   jobs[i] = null;
                }
            }
        }
        protected abstract void SingleThreadMergeWork(object job1, object job2);
        protected abstract void SingleThreadGroupByWork(object job);

        #region Jobs
        protected class GroupByJob
        {
            public RowHasher hasher;
            public Aggregate[] aggregates;
            public ITableResults resTable;
            public int start;
            public int end;

            protected GroupByJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end)
            {
                this.hasher = hasher;
                this.aggregates = aggregates;
                this.resTable = resTable;
                this.start = start;
                this.end = end;
            }
        }

        #endregion Jobs

        protected abstract GroupByResults CreateGroupByResults(GroupByJob job);

    }
}
