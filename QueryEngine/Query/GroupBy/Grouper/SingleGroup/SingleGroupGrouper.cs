using System.Collections.Generic;
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
        public SingleGroupGrouper(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper, false)
        {}

        /// <summary>
        /// Sets values to the Count(*) aggregates because there is nothing to be computed.
        /// Finds the non Count(*) aggregates and if there are some, they are further passed into
        /// grouping (parallel or single thread) based on InParallel flag, altogether with corresponding aggregate results. 
        /// Note that the we are passing direct reference to the aggregates results and aggregates. Thus it assumes
        /// that the further methods merge data only into the passed aggregate results.
        /// </summary>
        public override GroupByResults Group(ITableResults resTable)
        {
            var nonAsterixCountAggregates = new List<Aggregate>();
            var nonAsterixAggResults = new List<AggregateBucketResult>();
            var aggResults = AggregateBucketResult.CreateBucketResults(this.aggregates);

            for (int i = 0; i < this.aggregates.Length; i++)
            {
                if (this.aggregates[i].IsAstCount)
                {
                    // Actualise Count(*).
                    ((Count)aggregates[i]).IncBy(resTable.NumberOfMatchedElements, aggResults[i]);
                }
                else
                {
                    // Non astrix counts are further passed into the computatoin functions.
                    nonAsterixCountAggregates.Add(this.aggregates[i]);
                    nonAsterixAggResults.Add(aggResults[i]);
                }
            }

            // Note that the result will reside in the aggResults variable after the computation is finished.
            if (nonAsterixCountAggregates.Count != 0)
            {
                // If work can be split equaly use parallel sol. (Split equaly means that each thread will receive at least one portion of the result table.)
                if (this.InParallel && (resTable.NumberOfMatchedElements / this.ThreadCount > 0)) this.ParallelGroupBy(resTable, nonAsterixCountAggregates.ToArray(), nonAsterixAggResults.ToArray());
                else this.SingleThreadGroupBy(resTable, nonAsterixCountAggregates.ToArray(), nonAsterixAggResults.ToArray());
            } 
            return CreateGroupByResults(aggResults, resTable);
        }

        /// <summary>
        /// Computes groups in parallel. 
        /// Each thread gets a fair share of results from the result table.
        /// The passed list of aggregates resides on the last position in the jobs array.
        /// When the tasks are finished the results are merged single threaded onto the 
        /// last position in the jobs array.
        /// That means, that the results when the function returns are stored in the passed parameter
        /// of aggregates.
        /// </summary>
        /// <param name="results"> A result table from match clause. </param>
        /// <param name="aggs"> Aggregation functions. </param>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. </param>
        private void ParallelGroupBy(ITableResults results, Aggregate[] aggs, AggregateBucketResult[] aggResults)
        {
            // -1 because the main thread works as well
            Task[] tasks = new Task[this.ThreadCount - 1];

            // Create jobs
            var jobs = CreateJobs(results, aggs, aggResults);
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
        /// 
        /// Note that the passed aggregates results, are the ones that the rest will be merged into.
        /// They are expected to be at the last index of the jobs => they must have at least one result assigned.
        /// </summary>
        /// <param name="results"> A place to store aggregation results. </param>
        /// <param name="aggs"> Aggregation functions. </param>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. It is placed into the last job. </param>
        private GroupByJob[] CreateJobs(ITableResults results, Aggregate[] aggs, AggregateBucketResult[] aggResults)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            // No that this is never <= 0 because it was checked when picking the impl.
            int addition = results.NumberOfMatchedElements / this.ThreadCount;

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                jobs[i] = new GroupByJob(aggs, AggregateBucketResult.CreateBucketResults(aggs), current, current + addition, results);
                current += addition;
            }

            jobs[jobs.Length - 1] = new GroupByJob(aggs, aggResults, current, results.NumberOfMatchedElements, results);
            return jobs;
        }

        /// <summary>
        /// Computes single threadedly aggregates.
        /// </summary>
        /// <param name="results"> A place to store aggregation results. </param>
        /// <param name="aggs"> Aggregation functions. </param>
        /// <param name="aggResults"> The results of the merge is stored in this isntances. </param>
        private void SingleThreadGroupBy(ITableResults results, Aggregate[] aggs, AggregateBucketResult[] aggResults)
        {
            var job = new GroupByJob(aggs, aggResults, 0, results.NumberOfMatchedElements, results);
            SingleThreadGroupByWork(job);
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
                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].Apply(in tmpRow, aggResults[j]);
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
                for (int j  = 0; j < lastJob.aggregates.Length; j++)
                {
                    if (jobs[i].start != jobs[i].end)
                       lastJob.aggregates[j].Merge(lastJob.aggResults[j],  // to
                                                     jobs[i].aggResults[j]); // from
                }
            }
        }
        
        private class GroupByJob
        {
            public Aggregate[] aggregates;
            public AggregateBucketResult[] aggResults;
            public int start;
            public int end;
            public ITableResults results;

            public GroupByJob(Aggregate[] aggs, AggregateBucketResult[] aggRes, int start, int end, ITableResults res)
            {
                this.aggregates = aggs;
                this.start = start;
                this.end = end;
                this.results = res;
                this.aggResults = aggRes;
            }
        }

        private GroupByResults CreateGroupByResults(AggregateBucketResult[] bucket, ITableResults results)
        {
            var tmpDict = new Dictionary<GroupDictKey, AggregateBucketResult[]>();
            tmpDict.Add(new GroupDictKey(0, 0), bucket);
            return new DictGroupDictKeyBucket(tmpDict, results);
        }
    }
}
