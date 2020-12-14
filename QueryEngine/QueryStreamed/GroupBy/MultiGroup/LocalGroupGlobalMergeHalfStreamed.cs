using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine 
{
    /// <summary>
    /// Class represents a half streamed grouping if group by is set in the input query.
    /// The computation works as follows:
    /// Each matcher computes its groups localy and stores results of the matcher only if they 
    /// represent a representant of a group. If not, only aggregates are computed with the result.
    /// When matcher finishes, it merges its local results into a global groups.
    /// Notice that the keys of the global dictionary contain row proxies, this enables
    /// to obtain a keys that stem from different tables.
    /// </summary>
    internal class LocalGroupGlobalMergeResultProcessorHalfStreamed : GroupResultProcessor
    {
        private MatcherJob[] matcherJobs;
        private ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]> globalGroups;
        private Func<GroupDictKeyFull, AggregateBucketResult[]> bucketFactory;

        public LocalGroupGlobalMergeResultProcessorHalfStreamed(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper): base(aggs, hashes, helper)
        {
            this.matcherJobs = new MatcherJob[helper.ThreadCount];
            // Create initial job, comps and hashers
            this.CreateHashersAndComparers(out List<ExpressionEqualityComparer> equalityComparers, out List<ExpressionHasher> hashers);
            var firstComp = new RowEqualityComparerGroupKey(null, equalityComparers);
            var firstHasher = new RowHasher(hashers);
            firstComp.SetCache(firstHasher);
            firstHasher.SetCache(firstComp.Comparers);
            this.matcherJobs[0] = new MatcherJob(helper.ColumnCount, firstComp, firstHasher);

            for (int i = 1; i < ThreadCount; i++)
            {
                this.CloneHasherAndComparer(firstComp, firstHasher, out RowEqualityComparerGroupKey newComp, out RowHasher newHasher);
                matcherJobs[i] = new MatcherJob(helper.ColumnCount, newComp, newHasher);
            }
            this.bucketFactory = (GroupDictKeyFull x) => { return AggregateBucketResult.CreateBucketResults(this.aggregates); };
            var globalGroups = new ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]>(new RowEqualityComparerGroupDickKeyFull(firstComp.Clone().Comparers)); // to do add the shit
        }

        public override void Process(int matcherID, Element[] result)
        {
            var tmpJob = this.matcherJobs[matcherID];
            if (result != null)
            {
                // Create a temporary row.
                tmpJob.results.temporaryRow = result;
                int position = tmpJob.results.RowCount;
                TableResults.RowProxy row = tmpJob.results[position];
                var key = new GroupDictKey(tmpJob.hasher.Hash(in row), position); // It's a struct.

                if (!tmpJob.groups.TryGetValue(key, out position))
                {
                    position = tmpJob.groups.Count;
                    tmpJob.groups.Add(key, position);
                    // Store the temporary row in the table. This causes copying of the row to the actual lists of table.
                    // While the position of the stored row proxy remains the same, next time someone tries to access it,
                    // it returns the elements from the actual table and not the temporary row.
                    tmpJob.results.StoreTemporaryRow();
                    tmpJob.results.temporaryRow = null;
                }
                for (int j = 0; j < aggregates.Count; j++)
                    aggregates[j].Apply(in row, tmpJob.aggResults[j], position);
            } else
            {
                foreach (var item in tmpJob.groups)
                {
                    var keyFull = new GroupDictKeyFull(item.Key.hash, tmpJob.results[item.Key.position]);
                    var buckets = this.globalGroups.GetOrAdd(keyFull, bucketFactory);
                    for (int j = 0; j < aggregates.Count; j++)
                        aggregates[j].MergeThreadSafe(buckets[j], tmpJob.aggResults[j], item.Value);
                }
                this.matcherJobs[matcherID] = null;
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            throw new NotImplementedException();
        }


        private class MatcherJob
        {
            public TableResults results;
            public Dictionary<GroupDictKey, int> groups;
            public List<AggregateListResults> aggResults;
            public RowHasher hasher;

            public MatcherJob(int columnCount, RowEqualityComparerGroupKey comparer, RowHasher hasher)
            {
                this.results = new TableResults(columnCount);
                comparer.Results = this.results;
                this.groups = new Dictionary<GroupDictKey, int>(comparer);
                this.hasher = hasher;
            }
        }
        private void CloneHasherAndComparer(RowEqualityComparerGroupKey comparer, RowHasher hasher, out RowEqualityComparerGroupKey retComparer, out RowHasher retHasher)
        {
            retComparer = comparer.Clone();
            retHasher = hasher.Clone();
            retComparer.SetCache(retHasher);
            retHasher.SetCache(retComparer.Comparers);
        }

    }
}
