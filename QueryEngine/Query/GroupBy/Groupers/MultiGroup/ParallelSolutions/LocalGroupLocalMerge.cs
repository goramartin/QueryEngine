using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// The class represents multi group grouping algorithm.
    /// The class uses aggregations with array like storage or buckets.
    /// Each thread receives an equality comparer, hasher, aggregates and range of vertices.
    /// The threads then work independently on each other. When the threads finish, the results are merged.
    /// The results are merged in a form of a binary tree (similar to a merge sort).
    /// </summary>
    internal class LocalGroupLocalMerge : Grouper
    {
        public LocalGroupLocalMerge(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage) 
        { }

        public override AggregateResults Group(ITableResults resTable)
        {
            // Create hashers and equality comparers.
            CreateHashersAndComparers(out List<ExpressionEqualityComparer> equalityComparers, out List<ExpressionHasher> hashers);
            if (this.InParallel && ((resTable.NumberOfMatchedElements / this.ThreadCount) > 1)) return ParallelGroupBy(resTable, equalityComparers, hashers);
            else return SingleThreadGroupBy(resTable, equalityComparers, hashers);
        }

        private AggregateResults SingleThreadGroupBy(ITableResults resTable, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            var tmpComparer = new RowEqualityComparerGroupKey(resTable, equalityComparers);
            var tmpHasher = new RowHasher(hashers);
            tmpComparer.SetCache(tmpHasher);
            tmpHasher.SetCache(tmpComparer.Comparers);
            object tmpJob;

            if (!this.BucketStorage) tmpJob = new GroupByJobLists(tmpHasher, tmpComparer, this.aggregates, resTable, 0, resTable.NumberOfMatchedElements);
            else tmpJob = new GroupByJobBuckets(tmpHasher, tmpComparer, this.aggregates, resTable, 0, resTable.NumberOfMatchedElements);
            SingleThreadGroupByWork(tmpJob, this.BucketStorage);
            
            return null;
        }

        private AggregateResults ParallelGroupBy(ITableResults resTable, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            GroupByJob[] jobs = CreateJobs(resTable, this.aggregates, equalityComparers, hashers);
            ParallelGroupByWork(jobs, 0, ThreadCount, this.BucketStorage);
            //return jobs[0].aggResults;
            return null;
        }

        /// <summary>
        /// Creates jobs for the parallel group by.
        /// Note that the last job in the array has the end set to the end of the result table.
        /// The addition must always be > 0.
        /// Each job will receive a range from result table, hasher, comparer and aggregates.
        /// Note that they are all copies, because they contain a private stete (hasher contains reference to the equality comparers to enable caching when computing the hash.
        /// The comparers and hashers build in the constructor of this class are given to the last job, just like the aggregates passed to the construtor.
        /// </summary>
        private GroupByJob[] CreateJobs(ITableResults results, List<Aggregate> aggs, List<ExpressionEqualityComparer> equalityComparers, List<ExpressionHasher> hashers)
        {
            GroupByJob[] jobs = new GroupByJob[this.ThreadCount];
            int current = 0;
            int addition = results.NumberOfMatchedElements / this.ThreadCount;
            if (addition == 0)
                throw new ArgumentException($"{this.GetType()}, a range for a thread cannot be 0.");
             
            // Set their internal cache.
            var lastComp = new RowEqualityComparerGroupKey(results, equalityComparers);
            var lastHasher = new RowHasher(hashers);
            lastComp.SetCache(lastHasher);
            lastHasher.SetCache(lastComp.Comparers);

            for (int i = 0; i < jobs.Length - 1; i++)
            {
                var tmpComp = lastComp.Clone(); 
                var tmpHash = lastHasher.Clone();
                tmpComp.SetCache(tmpHash); 
                tmpHash.SetCache(tmpComp.Comparers);
                if (!this.BucketStorage) jobs[i] = new GroupByJobLists(tmpHash, tmpComp, aggs, results, current, current + addition);
                else jobs[i] = new GroupByJobBuckets(tmpHash, tmpComp, aggs, results, current, current + addition);
                current += addition;
            }
           
            if (!this.BucketStorage) jobs[jobs.Length - 1] = new GroupByJobLists(lastHasher, lastComp, aggs, results, current, results.NumberOfMatchedElements);
            else jobs[jobs.Length - 1] = new GroupByJobBuckets(lastHasher, lastComp, aggs, results, current, results.NumberOfMatchedElements);
            return jobs;
        }

        /// <summary>
        /// Creates a binary tree, where on each non leaf level, results from threads are merged. On the 
        /// last level before the leaf level a grouping is started for each thread based on the given range of result table.
        /// And on the same level they are also merged.
        /// That means that the recursion never reaches the leaf level by itself but the leaves represent grouping computations started 
        /// from the last level before leaf level.
        /// The results are merged on the jobs[0].
        /// </summary>
        /// <param name="jobs"> Jobs for each thread. </param>
        /// <param name="start"> Starting index of a jobs to finish. </param>
        /// <param name="end"> End index of a jobs to finish. </param>
        /// <param name="bucketStorage"> Whether to compute with bucket method versions. </param>
        private static void ParallelGroupByWork(GroupByJob[] jobs, int start, int end, bool bucketStorage)
        {
            if (end - start > 3)
            {
                // compute middle of the range
                int middle = ((end - start) / 2) + start;
                if (middle % 2 == 1) middle--;

                Task task = Task.Factory.StartNew(() => ParallelGroupByWork(jobs, middle, end, bucketStorage));
                // Current thread work.
                ParallelGroupByWork(jobs, start, middle, bucketStorage);

                // Wait for the other task to finish and start merging its results with yours.
                task.Wait();
                SingleThreadMergeWork(jobs[start], jobs[middle], bucketStorage);
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
                    tasks[taskIndex] = Task.Factory.StartNew(() => SingleThreadGroupByWork(tmp, bucketStorage));
                    taskIndex++;
                }
                SingleThreadGroupByWork(jobs[start], bucketStorage);
                Task.WaitAll(tasks);
                
                // Merge the results from the leaf level.
                for (int i = start + 1; i < end; i++)
                {
                   SingleThreadMergeWork(jobs[start], jobs[i], bucketStorage);
                   jobs[i] = null;
                }
            }
        }
       
        private static void SingleThreadMergeWork(object job1, object job2, bool bucketStorage)
        {
            if (bucketStorage) SingleThreadMergeWorkWithBuckets(job1, job2);
            else SingleThreadMergeWorkWithLists(job1, job2);
        }
        
        private static void SingleThreadGroupByWork(object job, bool bucketStorage) 
        {
            if (bucketStorage) SingleThreadGroupByWorkWithBuckets(job);
            else SingleThreadGroupByWorkWithLists(job);
        }


        #region WithBuckets
       
        /// <summary>
        /// Main work of a thread when merging with another threads groups.
        /// And then for each entry from the other dictionary try to add it.
        /// Merge only if there is already the given group.
        /// </summary>
        private static void SingleThreadMergeWorkWithBuckets(object job1, object job2)
        {
            #region DECL
            var groups1 = ((GroupByJobBuckets)job1).groups;
            var groups2 = ((GroupByJobBuckets)job2).groups;
            var aggregates = ((GroupByJobBuckets)job1).aggregates;
            AggregateBucketResult[] buckets;
            #endregion DECL

            foreach (var item in groups2)
            {
                if (!groups1.TryGetValue(item.Key, out buckets))
                {
                    groups1.Add(item.Key, item.Value);
                    // No need to merge the results.
                    continue; 
                }
                // It merges the results only if the group was already in the dictionary.
                for (int i = 0; i < aggregates.Count; i++)
                    aggregates[i].Merge(buckets[i], item.Value[i]);
            }
        }

        /// <summary>
        /// Main work of a thread when grouping.
        /// For each result row.
        /// Try to add it to the dictionary and apply aggregate functions with the rows.
        /// Note that when the hash is computed. The comparer cache is set.
        /// So when the insertion happens, it does not have to compute the values for comparison.
        /// </summary>
        private static void SingleThreadGroupByWorkWithBuckets(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobBuckets)job);
            var hasher = tmpJob.hasher;
            var aggregates = tmpJob.aggregates;
            var results = tmpJob.results;
            var groups = tmpJob.groups;
            AggregateBucketResult[] buckets;
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.

                if (!groups.TryGetValue(key, out buckets))
                {
                    buckets = AggregateBucketResult.CreateBucketResults(aggregates);
                    groups.Add(key, buckets);
                }

                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in row, buckets[j]);
            }
        }

        #endregion WithBuckets

        #region WithLists
       
        /// <summary>
        /// Main work of a thread when merging with another threads groups.
        /// For each entry from the other dictionary a method MergeOn(...)
        /// is called, which either combines the results of the two groups or adds it to the end of the result list.
        /// Also, if both groups exists in the both jobs, they are combined.
        /// Otherwise the new entry is added to the dictionary.
        /// </summary>
        private static void SingleThreadMergeWorkWithLists(object job1, object job2)
        {
            #region DECL
            var groups1 = ((GroupByJobLists)job1).groups;
            var groups2 = ((GroupByJobLists)job2).groups;
            var aggs1 = ((GroupByJobLists)job1).aggregates;
            var aggsResults1 = ((GroupByJobLists)job1).aggResults;
            var aggsResults2 = ((GroupByJobLists)job2).aggResults;
            #endregion DECL

            foreach (var item in groups2)
            {
                if (!groups1.TryGetValue(item.Key, out int position))
                {
                    position = groups1.Count;
                    groups1.Add(item.Key, position);
                }
                for (int i = 0; i < aggs1.Count; i++)
                    aggs1[i].Merge(aggsResults1[i], position, aggsResults2[i], item.Value);
            }
        }

        /// <summary>
        /// Main work of a thread when grouping.
        /// For each result row.
        /// Try to add it to the dictionary and apply aggregate functions with the rows.
        /// Note that when the hash is computed. The comparer cache is set.
        /// So when the insertion happens, it does not have to compute the values for comparison.
        /// </summary>
        private static void SingleThreadGroupByWorkWithLists(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobLists)job);
            var hasher = tmpJob.hasher;
            var aggregates = tmpJob.aggregates;
            var results = tmpJob.results;
            var groups = tmpJob.groups;
            var aggResults = tmpJob.aggResults;
            int position;
            TableResults.RowProxy row;
            GroupDictKey key;
            #endregion DECL

            for (int i = tmpJob.start; i < tmpJob.end; i++)
            {
                row = results[i];
                key = new GroupDictKey(hasher.Hash(in row), i); // It's a struct.
                if (!groups.TryGetValue(key, out position))
                {
                    position = groups.Count;
                    groups.Add(key, position);
                }

                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in row, aggResults[j], position);
            }
        }
        #endregion WithLists

        #region Jobs
        private class GroupByJob
        {
            public RowHasher hasher;
            public List<Aggregate> aggregates;
            public ITableResults results;
            public int start;
            public int end;
            public bool bucketStorage;

            protected GroupByJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, List<Aggregate> aggregates, ITableResults results, int start, int end, bool bucketStorage)
            {
                this.hasher = hasher;
                this.aggregates = aggregates;
                this.results = results;
                this.start = start;
                this.end = end;
                this.bucketStorage = bucketStorage;
            }
        }

        private class GroupByJobBuckets : GroupByJob
        {
            public Dictionary<GroupDictKey, AggregateBucketResult[]> groups;

            public GroupByJobBuckets(RowHasher hasher, RowEqualityComparerGroupKey comparer, List<Aggregate> aggregates, ITableResults results, int start, int end): base(hasher, comparer, aggregates, results, start, end, true)
            {
                this.groups = new Dictionary<GroupDictKey, AggregateBucketResult[]>(comparer);
            }
        }

        private class GroupByJobLists: GroupByJob
        {
            public Dictionary<GroupDictKey, int> groups;
            public List<AggregateListResults> aggResults;
            public GroupByJobLists(RowHasher hasher, RowEqualityComparerGroupKey comparer, List<Aggregate> aggregates, ITableResults results, int start, int end) : base(hasher, comparer, aggregates, results, start, end, false)
            {
                this.groups = new Dictionary<GroupDictKey, int>(comparer);
                this.aggResults = AggregateListResults.CreateArrayResults(aggregates);
            }
        }
        #endregion Jobs


    }
}
