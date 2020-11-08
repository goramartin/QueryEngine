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
    /// </summary>
    internal class SingleGroupGrouper : Grouper
    {
        private List<AggregateArray> arrayAggregates = null;
        public SingleGroupGrouper(List<Aggregate> aggs,List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        { }

        /// <summary>
        /// Sets values to the Count(*) aggregates because there is nothing to be computed.
        /// Finds the non Count(*) aggregates and if there are some, they are further passed into
        /// grouping (parallel or single thread) based on InParallel flag, altogether with corresponding aggregate results. 
        /// Note that the we are passing direct reference to the aggregates results and aggregates. Thus it assumes
        /// that the further methods merge data only into the passed aggregate results.
        /// </summary>
        public override List<AggregateArrayResults> Group(ITableResults resTable)
        {
            this.arrayAggregates = (List<AggregateArray>)this.aggregates.Cast<AggregateArray>();
            var nonAsterixCountAggregates = new List<AggregateArray>();
            var nonAsterixAggResults = new List<AggregateArrayResults>();
            var aggResults = AggregateArrayResults.CreateArrayResults(this.arrayAggregates);

            for (int i = 0; i < this.arrayAggregates.Count; i++)
            {
                if (this.arrayAggregates[i].IsAstCount)
                {
                    // Actualise Count(*) result array.
                    this.arrayAggregates[i].SetAggResults(aggResults[i]);
                    ((ArrayCount)arrayAggregates[i]).IncBy(resTable.NumberOfMatchedElements, 0);
                }
                else
                {
                    // Non astrix counts are further passed into the computatoin functions.
                    nonAsterixCountAggregates.Add(this.arrayAggregates[i]);
                    nonAsterixAggResults.Add(aggResults[i]);
                }
            }

            // Note that the result will reside in the aggResults variable after the computation is finished.
            if (nonAsterixCountAggregates.Count == 0) return aggResults; 
            else if (this.InParallel) this.ParallelGroupBy(resTable, nonAsterixCountAggregates, nonAsterixAggResults);
            else this.SingleThreadGroupBy(resTable, nonAsterixCountAggregates, nonAsterixAggResults);

            return aggResults;
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
        /// <param name="aggResults"> The results of the merge is stored in this isntances. </param>
        private void ParallelGroupBy(ITableResults results, List<AggregateArray> aggs, List<AggregateArrayResults> aggResults)
        {
            // -1 because the main thread works as well
            Task[] tasks = new Task[this.ThreadCount - 1];
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];

            // Create jobs
            jobs = CreateJobs(results, aggs, aggResults);
            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = jobs[i];
                tasks[i] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp));
            }

            // The main thread works with the last job in the array.
            SingleThreadGroupByWork(jobs[jobs.Length - 1]);
            Task.WaitAll(tasks);
            // Merge doesnt have to be in parallel because it s grouping only (#thread) values.
            MergeRows(jobs);
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
        /// <param name="aggResults"> The results of the merge is stored in this isntances. It is placed into the last job. </param>
        private GroupByJob[] CreateJobs(ITableResults results, List<AggregateArray> aggs, List<AggregateArrayResults> aggResults)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJob(aggs.CloneAggs(), AggregateArrayResults.CreateArrayResults(aggs), current, current + addition, results);
                current += addition;
            }

            // Create the last job, this fixes the problem if addition is 0, in that case, it doesnt matter
            // that the work will be done by one thread entirely because the res table is way to small to begin with.
            jobs[jobs.Length - 1] = new GroupByJob(aggs, aggResults, current, results.NumberOfMatchedElements, results);
            return jobs;
        }

        /// <summary>
        /// Computes single threadedly aggregates.
        /// </summary>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. </param>
        private void SingleThreadGroupBy(ITableResults results, List<AggregateArray> aggs, List<AggregateArrayResults> aggResults)
        {
            var job = new GroupByJob(aggs, aggResults, 0, results.NumberOfMatchedElements, results);
            SingleThreadGroupByWork(job);
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

        /// <summary>
        /// Merges results of grouping onto the position in jobs.
        /// Everything is merged into the last job.
        /// </summary>
        private void MergeRows(GroupByJob[] jobs)
        {
            for (int i = 0; i < jobs.Length-1; i++)
            {
                for (int j  = 0; j < jobs[0].aggregates.Count; j++)
                {
                    if (jobs[i].start != jobs[i].end)
                    {
                        jobs[jobs.Length - 1].aggregates[j].SetMergingWith(jobs[i].aggResults[j]);
                        jobs[jobs.Length - 1].aggregates[j].MergeOn(0, 0);     
                    }
                }
            }
        }
        
        private class GroupByJob
        {
            public List<AggregateArray> aggregates;
            public List<AggregateArrayResults> aggResults;
            public int start;
            public int end;
            public ITableResults results;

            public GroupByJob(List<AggregateArray> aggs, List<AggregateArrayResults> aggRes, int start, int end, ITableResults res)
            {
                this.aggregates = aggs;
                this.start = start;
                this.end = end;
                this.results = res;
                this.aggResults = aggRes;

                // Set results arrays directly into the aggregates.
                for (int i = 0; i < this.aggregates.Count; i++)
                    this.aggregates[i].SetAggResults(this.aggResults[i]);

            }
        }
    }
}
