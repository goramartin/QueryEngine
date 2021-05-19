using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// A class represents a multigroup grouping algorithm.
    /// The grouping is done using ConcurrentDictionary.
    /// The groups are computed globally.
    /// The class should be used only as the parallel solution and not with thread count set to 1.
    /// The class uses aggretation with buckets or array like storage.
    /// Each thread recieves an equal portion of the results from the result table.
    /// Subsequently, the threads start to aggregate the results using ConcurrentDictionary (hence the name global, since a global map is used).
    /// </summary>
    internal abstract class GlobalGroupBy : Grouper
    {
        public GlobalGroupBy(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        public override GroupByResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            // The hashers receive also the equality comparer as cache.
            CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers);
            return this.ParallelGroupBy(RowEqualityComparerInt.Factory(resTable, comparers, new RowHasher(hashers), false), resTable);
        }

        private GroupByResults ParallelGroupBy(RowEqualityComparerInt equalityComparer, ITableResults resTable)
        {
            var jobs = CreateJobs(equalityComparer, resTable);
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
        protected abstract void SingleThreadGroupByWork(object job);

        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// Each job will receive a range from result table, aggregates and a global place to store groups and the aggregated values.
        /// Note that there is a single comparer for the ConcurrentDictionary, thus no caching of the expression is done.
        /// </summary>
        private GroupByJob[] CreateJobs(RowEqualityComparerInt equalityComparer, ITableResults resTable)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = resTable.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");

            return CreateSpecJobs(jobs, equalityComparer, resTable, current, addition);
        }
        
        protected abstract GroupByJob[] CreateSpecJobs(GroupByJob[] jobs, RowEqualityComparerInt equalityComparer, ITableResults resTable, int current, int addition);
        protected abstract GroupByResults CreateGroupByResults(GroupByJob job);
        protected abstract class GroupByJob
        {
            public Aggregate[] aggregates;
            public ITableResults resTable;
            public int start;
            public int end;
            public GroupByJob(Aggregate[] aggregates, ITableResults resTable, int start, int end)
            {
                this.aggregates = aggregates;
                this.resTable = resTable;
                this.start = start;
                this.end = end;
            }
        }
    }
     
}
