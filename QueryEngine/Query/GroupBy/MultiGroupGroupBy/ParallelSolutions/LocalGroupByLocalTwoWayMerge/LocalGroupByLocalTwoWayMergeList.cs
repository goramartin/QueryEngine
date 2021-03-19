using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class LocalGroupByLocalTwoWayMergeList : LocalGroupByLocalTwoWayMerge
    {
        public LocalGroupByLocalTwoWayMergeList(Aggregate[] aggs, ExpressionHolder[] hashes, IGroupByExecutionHelper helper, bool useBucketStorage) : base(aggs, hashes, helper, useBucketStorage)
        { }

        #region WithLists

        /// <summary>
        /// Main work of a thread when merging with another threads groups.
        /// For each entry from the other dictionary a method MergeOn(...)
        /// is called, which either combines the results of the two groups or adds it to the end of the result list.
        /// Also, if both groups exists in the both jobs, they are combined.
        /// Otherwise the new entry is added to the job1's dictionary.
        /// </summary>
        /// <param name="job1"> A place to merge into. </param>
        /// <param name="job2"> A place to merge from. </param>
        protected override void SingleThreadMergeWork(object job1, object job2)
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
                for (int i = 0; i < aggs1.Length; i++)
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
        protected override void SingleThreadGroupByWork(object job)
        {
            #region DECL
            var tmpJob = ((GroupByJobLists)job);
            var hasher = tmpJob.hasher;
            var aggregates = tmpJob.aggregates;
            var results = tmpJob.resTable;
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

                for (int j = 0; j < aggregates.Length; j++)
                    aggregates[j].Apply(in row, aggResults[j], position);
            }
        }
        #endregion WithLists


        protected override GroupByJob CreateJob(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end)
        {
            return new GroupByJobLists(hasher, comparer, aggregates, resTable, start, end);
        }

        private class GroupByJobLists : GroupByJob
        {
            public Dictionary<GroupDictKey, int> groups;
            public AggregateListResults[] aggResults;
            public GroupByJobLists(RowHasher hasher, RowEqualityComparerGroupKey comparer, Aggregate[] aggregates, ITableResults resTable, int start, int end) : base(hasher, comparer, aggregates, resTable, start, end)
            {
                this.groups = new Dictionary<GroupDictKey, int>(comparer);
                this.aggResults = AggregateListResults.CreateListResults(aggregates);
            }
        }

        protected override GroupByResults CreateGroupByResults(GroupByJob job)
        {
            var tmp = (GroupByJobLists)job;
            return new GroupByResultsList(tmp.groups, tmp.aggResults, tmp.resTable);
        }
    }
}
