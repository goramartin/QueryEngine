using System;
using System.Collections.Concurrent;
using System.Threading;

namespace QueryEngine
{
    internal sealed class GlobalGroupByArray : GlobalGroupBy
    {
        public GlobalGroupByArray(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        /// <summary>
        /// A main work of each thread when grouping.
        /// For each result row, add/get a group in/from the global dictionary and compute the
        /// corresponding aggregate values for the group.
        /// The values are stored using arrays (an index corresponding to a group results is placed as a value on a key, the results can be then accessed via the stored index).
        /// Firstly, a position is inserted into the dictionary, note that the constructor
        /// runs outside of the synchronization, so it can happen that other thread inserted an element in the meanwhile.
        /// Then, if the position does exceed the array lenght, it acquires entire semaphore and doubles the arrays.
        /// Otherwise, it acquires only one part of the semaphore, updates the aggregates on the given position and releases the semaphore's part.
        /// The aggregate values corresponding to a group are stored on the index stored in the ConcurrentDictionary.
        /// </summary>
        /// <param name="job"> A group by job class. </param>
        protected override void SingleThreadGroupByWork(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobArrays)job);
            var results = tmpJob.resTable;
            var groups = tmpJob.groups;
            var aggregates = tmpJob.aggregates;
            var semaphore = tmpJob.semaphore;
            Func<int, int> positionFactory = tmpJob.positionFactory;
            var aggResults = tmpJob.aggResults;
            int position;
            TableResults.RowProxy row;
            int threadCount = tmpJob.threadCount;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                position = groups.GetOrAdd(i, positionFactory);

                if (aggregates.Length == 0) continue;
                else
                {
                    if (aggResults[0].ArraySize() <= position)
                    {
                        lock (groups)
                        {
                            if (aggResults[0].ArraySize() <= position)
                            {
                                // Acquire the entire semaphore.
                                for (int enters = 0; enters < threadCount; enters++)
                                    semaphore.WaitOne();
                                // Double the array sizes
                                for (int j = 0; j < aggResults.Length; j++)
                                    aggResults[j].DoubleSize(position);

                                // Release the entire semaphore
                                semaphore.Release(threadCount);
                            }
                        }
                    }

                    semaphore.WaitOne();

                    for (int j = 0; j < aggregates.Length; j++)
                        aggregates[j].ApplyThreadSafe(in row, aggResults[j], position);

                    semaphore.Release();
                }
            }
        }

        protected override GroupByJob[] CreateSpecJobs(GroupByJob[] jobs, RowEqualityComparerInt equalityComparer, ITableResults resTable, int current, int addition)
        {
            var aggResults = AggregateArrayResults.CreateArrayResults(this.aggregates);
            var concurrentDictArrays = new ConcurrentDictionary<int, int>(equalityComparer);
            int capture = 0;
            Func<int, int> positionFactory = (int x) => { return Interlocked.Increment(ref capture); };
            Semaphore semaphore = new Semaphore(this.ThreadCount, this.ThreadCount);

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJobArrays(concurrentDictArrays, this.aggregates, resTable, current, current + addition, aggResults, positionFactory, semaphore, jobs.Length);
                current += addition;
            }
            jobs[jobs.Length - 1] = new GroupByJobArrays(concurrentDictArrays, this.aggregates, resTable, current, resTable.NumberOfMatchedElements, aggResults, positionFactory, semaphore, jobs.Length);
            return jobs;
        }

        private class GroupByJobArrays : GroupByJob
        {
            public ConcurrentDictionary<int, int> groups;
            public AggregateArrayResults[] aggResults;
            public Func<int, int> positionFactory;
            public Semaphore semaphore;
            public int threadCount;

            public GroupByJobArrays(ConcurrentDictionary<int, int> groups, Aggregate[] aggregates, ITableResults resTable, int start, int end, AggregateArrayResults[] aggResults, Func<int, int> positionFactory, Semaphore semaphore, int threadCount) : base(aggregates, resTable, start, end)
            {
                this.groups = groups;
                this.aggResults = aggResults;
                this.positionFactory = positionFactory;
                this.semaphore = semaphore;
                this.threadCount = threadCount;
            }
        }

        protected override GroupByResults CreateGroupByResults(GroupByJob job)
        {
            var tmpJob = (GroupByJobArrays)job;
            return new GroupByResultsArray(tmpJob.groups, tmpJob.aggResults, tmpJob.resTable);
        }
    }
}
