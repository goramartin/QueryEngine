using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;

namespace QueryEngine
{
    /// <summary>
    /// A class that groups results only into one group.
    /// Is used when an aggregation is placed in the input query but no group by is set.
    /// In the single thread grouping, no clones of the aggregates are required.
    /// The final result is stored in the containing list field defined in the parent class.
    /// </summary>
    internal class SingleGroupGrouper : Grouper
    {
        private List<Aggregate> nonAsterixCountAggregates = new List<Aggregate>();

        public SingleGroupGrouper(List<Aggregate> aggs, IGroupByExecutionHelper helper) : base(aggs, helper) { }

        /// <summary>
        /// Sets values to the Count(*) aggregate because there is nothing to be computed.
        /// Finds the non Count(*) aggregates and there are some, they are further passed into
        /// grouping (parallel or single thread) based on InParallel flag. 
        /// Note that the we are passing direct reference to the aggregates. Thus it assumes
        /// that the further methods merge data onto the passed aggregates.
        /// </summary>
        public override List<Aggregate> Group(ITableResults resTable)
        {
            for (int i = 0; i < this.aggregates.Count; i++)
            {
                if (this.aggregates[i].IsAstCount) ((Count)this.aggregates[i]).IncBy(resTable.NumberOfMatchedElements, 0);
                else nonAsterixCountAggregates.Add(this.aggregates[i]);
            }

            if (nonAsterixCountAggregates.Count == 0) return this.aggregates;
            else if (this.InParallel) this.ParallelGroupBy(resTable, this.nonAsterixCountAggregates);
            else this.SingleThreadGroupBy(resTable, this.nonAsterixCountAggregates);

            return this.aggregates;
        }

        /// <summary>
        /// Computes groups in parallel. 
        /// Each thread gets a fair share of results from the result table.
        /// Jobs receive a clones of aggregates passed into the function.
        /// The passed list of aggregates resides on the last position in the jobs array.
        /// When the tasks are finished the results are merged single threaded onto the 
        /// last position in the jobs array.
        /// That means, that the results when the function returns are stored in the passed parameter
        /// of aggregates.
        /// </summary>
        private void ParallelGroupBy(ITableResults results, List<Aggregate> aggs)
        {
            // -1 because the main thread works as well
            Task[] tasks = new Task[this.ThreadCount - 1];
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];

            // Create jobs
            jobs = CreateJobs(results, aggs);
            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = jobs[i];
                tasks[i] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp));
            }

            // The main thread works with the last job in the array.
            SingleThreadGroupByWork(jobs[jobs.Length - 1]);
            Task.WaitAll(tasks);
            MergeRows(jobs);
        }

        /// <summary>
        /// Merges results of grouping onto the position in jobs.
        /// </summary>
        private void MergeRows(GroupByJob[] jobs)
        {
            for (int i = 0; i < jobs.Length-1; i++)
            {
                for (int j  = 0; j < jobs[0].aggregates.Count; j++)
                {
                    if (jobs[i].start != jobs[i].end)
                        jobs[jobs.Length - 1].aggregates[j].MergeOn(0, jobs[i].aggregates[j]);     
                }
            }
        }
        
        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the start/end set to the end of result table.
        /// If the addition == 0, the last job receives the entire result table. In terms of other values,
        /// each thread is given at least one result row.
        /// 
        /// Note that the passed aggregates, are the ones that the rest will be merged into.
        /// They are expected to be at the last index of the jobs => they must have at least one result assigned.
        /// </summary>
        private GroupByJob[] CreateJobs(ITableResults results, List<Aggregate> aggs)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJob(aggs.CloneAggs(), current, current + addition, results);
                current += addition;
            }

            // Create the last job, this fixes the problem if addition is 0, in that case, it doesnt matter
            // that the work will be done by one thread entirely because the res table is way to small to begin with.
            jobs[jobs.Length - 1] = new GroupByJob(aggs, current, results.NumberOfMatchedElements, results);
            return jobs;
        }

        /// <summary>
        /// Calls static method for group by computation.
        /// </summary>
        /// <param name="results"> Results from the match algorithm. </param>
        /// <param name="aggs"> Aggregates to compute. </param>
        private void SingleThreadGroupBy(ITableResults results, List<Aggregate> aggs)
        {
            var tmpJob = new GroupByJob(aggs, 0, results.NumberOfMatchedElements, results);
            SingleThreadGroupByWork(tmpJob);
        }

        /// <summary>
        /// Serves as a work to a single thread.
        /// For each result row from the results table in the given range.
        /// Compute the aggregates with the row for each aggregate.
        /// </summary>
        /// <param name="job"> A GroupByJob class. </param>
        private static void SingleThreadGroupByWork(Object job)
        {
            var groupByJob = (GroupByJob)job;
            var results = groupByJob.results;
            var aggregates = groupByJob.aggregates;

            for (int i = groupByJob.start; i < groupByJob.end; i++)
            {
                var tmpRow = results[i];
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in tmpRow, 0);
            }
        }

        private class GroupByJob
        {
            public List<Aggregate> aggregates;
            public int start;
            public int end;
            public ITableResults results;

            public GroupByJob(List<Aggregate> aggs, int start, int end, ITableResults res)
            {
                this.aggregates = aggs;
                this.start = start;
                this.end = end;
                this.results = res;
            }
        }
    }
}
