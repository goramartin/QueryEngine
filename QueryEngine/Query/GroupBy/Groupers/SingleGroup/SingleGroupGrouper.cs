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
        public SingleGroupGrouper(List<Aggregate> aggs,List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        { }

        /// <summary>
        /// Sets values to the Count(*) aggregates because there is nothing to be computed.
        /// Finds the non Count(*) aggregates and if there are some, they are further passed into
        /// grouping (parallel or single thread) based on InParallel flag, altogether with corresponding aggregate results. 
        /// Note that the we are passing direct reference to the aggregates results and aggregates. Thus it assumes
        /// that the further methods merge data only into the passed aggregate results.
        /// </summary>
        public override AggregateResults Group(ITableResults resTable)
        {
            var nonAsterixCountAggregates = new List<Aggregate>();
            var nonAsterixAggResults = new List<AggregateListResults>();
            var aggResults = AggregateListResults.CreateArrayResults(this.aggregates);

            for (int i = 0; i < this.aggregates.Count; i++)
            {
                if (this.aggregates[i].IsAstCount)
                {
                    // Actualise Count(*) result array.
                    ((Count)aggregates[i]).IncBy(resTable.NumberOfMatchedElements, aggResults[i], 0);
                }
                else
                {
                    // Non astrix counts are further passed into the computatoin functions.
                    nonAsterixCountAggregates.Add(this.aggregates[i]);
                    nonAsterixAggResults.Add(aggResults[i]);
                }
            }

            // Note that the result will reside in the aggResults variable after the computation is finished.
            if (nonAsterixCountAggregates.Count == 0) return null; //return aggResults; 
            else if (this.InParallel) return this.ParallelGroupBy(resTable, nonAsterixCountAggregates, nonAsterixAggResults);
            else return this.SingleThreadGroupBy(resTable, nonAsterixCountAggregates, nonAsterixAggResults);

            //return aggResults;
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
        /// <param name="results"> A place to store aggregation results. </param>
        /// <param name="aggs"> Aggregation functions. </param>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. </param>
        private AggregateResults ParallelGroupBy(ITableResults results, List<Aggregate> aggs, List<AggregateListResults> aggResults)
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

            return null;
        }
        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the start/end set to the end of result table.
        /// If the addition == 0, the last job receives the entire result table. In terms of other values,
        /// each thread is given at least one result row.
        /// 
        /// Note that the passed aggregates results, are the ones that the rest will be merged into.
        /// They are expected to be at the last index of the jobs => they must have at least one result assigned.
        /// </summary>
        /// <param name="results"> A place to store aggregation results. </param>
        /// <param name="aggs"> Aggregation functions. </param>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. It is placed into the last job. </param>
        private GroupByJob[] CreateJobs(ITableResults results, List<Aggregate> aggs, List<AggregateListResults> aggResults)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJob(aggs, AggregateListResults.CreateArrayResults(aggs), current, current + addition, results);
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
        /// <param name="results"> A place to store aggregation results. </param>
        /// <param name="aggs"> Aggregation functions. </param>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. </param>
        private AggregateResults SingleThreadGroupBy(ITableResults results, List<Aggregate> aggs, List<AggregateListResults> aggResults)
        {
            var job = new GroupByJob(aggs, aggResults, 0, results.NumberOfMatchedElements, results);
            SingleThreadGroupByWork(job);
            // return aggResults
            return null;
        }

        /// <summary>
        /// Serves as a work to a single thread.
        /// For each result row from the results table in the given range.
        /// Compute the aggregates with the row.
        /// </summary>
        /// <param name="job"> A GroupByJob class. </param>
        private static void SingleThreadGroupByWork(Object job)
        {

            #region DECL
            var groupByJob = (GroupByJob)job;
            var results = groupByJob.results;
            var aggregates = groupByJob.aggregates;
            var aggResults = groupByJob.aggResults;
            #endregion DECL

            for (int i = groupByJob.start; i < groupByJob.end; i++)
            {
                var tmpRow = results[i];
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in tmpRow, aggResults[j], 0);
            }
        }

        /// <summary>
        /// Merges results of grouping onto the position in jobs.
        /// Everything is merged into the last job.
        /// </summary>
        private void MergeRows(GroupByJob[] jobs)
        {
            var lastJob = jobs[jobs.Length - 1];
            for (int i = 0; i < jobs.Length-1; i++)
            {
                for (int j  = 0; j < lastJob.aggregates.Count; j++)
                {
                    if (jobs[i].start != jobs[i].end)
                       lastJob.aggregates[j].MergeOn(lastJob.aggResults[j], 0,  // to
                                                     jobs[i].aggResults[j], 0); // from
                }
            }
        }
        
        private class GroupByJob
        {
            public List<Aggregate> aggregates;
            public List<AggregateListResults> aggResults;
            public int start;
            public int end;
            public ITableResults results;

            public GroupByJob(List<Aggregate> aggs, List<AggregateListResults> aggRes, int start, int end, ITableResults res)
            {
                this.aggregates = aggs;
                this.start = start;
                this.end = end;
                this.results = res;
                this.aggResults = aggRes;
            }
        }
    }
}
