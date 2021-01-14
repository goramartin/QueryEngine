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
    /// The aggregate func. results are firstly stored in lists and then are put into buckets 
    /// when they are merged into the global dictionary.
    /// The computation works as follows:
    /// Each matcher computes its groups localy and stores results of the matcher only if they 
    /// represent a representant of a group. If not, only aggregates are computed with the result.
    /// When matcher finishes, it merges its local results into a global groups.
    /// Notice that the keys of the global dictionary contain row proxies, this enables
    /// to obtain a keys that stem from different tables.
    /// Notice that if it runs in single thread, the mergins does not happen. Thus we can use this class
    /// as a reference solution for the half streamed version using lists as a result storage.
    /// </summary>
    internal class LocalGroupGlobalMergeHalfStreamedListBucket : GroupResultProcessor
    {
        private Job[] matcherJobs;
        private ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]> globalGroups;

        public LocalGroupGlobalMergeHalfStreamedListBucket(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount) : base(expressionInfo, executionHelper, columnCount)
        {
            this.matcherJobs = new Job[this.executionHelper.ThreadCount];
           
            // Create initial job, comps and hashers
            this.CreateHashersAndComparers(out ExpressionEqualityComparer[] equalityComparers, out ExpressionHasher[] hashers);
            var firstComp = new RowEqualityComparerGroupKey(null, equalityComparers);
            var firstHasher = new RowHasher(hashers);
            firstComp.SetCache(firstHasher);
            firstHasher.SetCache(firstComp.Comparers);
            
            this.matcherJobs[0] = new Job(this.aggregates, this.ColumnCount, firstComp, firstHasher, AggregateBucketResult.CreateBucketResults(this.aggregates));
            for (int i = 1; i < this.executionHelper.ThreadCount; i++)
            {
                this.CloneHasherAndComparer(firstComp, firstHasher, out RowEqualityComparerGroupKey newComp, out RowHasher newHasher);
                matcherJobs[i] = new Job(this.aggregates, this.ColumnCount, newComp, newHasher, AggregateBucketResult.CreateBucketResults(this.aggregates));
            }

            this.globalGroups = new ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]>(new RowEqualityComparerGroupDickKeyFull(firstComp.Clone().Comparers));
        }

        public override void Process(int matcherID, Element[] result)
        {
            var tmpJob = this.matcherJobs[matcherID];
            if (result != null)
            {
                // Create a temporary row.
                tmpJob.results.temporaryRow = result;
                int rowPosition = tmpJob.results.RowCount;
                TableResults.RowProxy row = tmpJob.results[rowPosition];
                var key = new GroupDictKey(tmpJob.hasher.Hash(in row), rowPosition); // It's a struct.

                if (!tmpJob.groups.TryGetValue(key, out int resPosition))
                {
                    resPosition = tmpJob.groups.Count;
                    tmpJob.groups.Add(key, resPosition);
                    // Store the temporary row in the table. This causes copying of the row to the actual lists of table.
                    // While the position of the stored row proxy remains the same, next time someone tries to access it,
                    // it returns the elements from the actual table and not the temporary row.
                    tmpJob.results.StoreTemporaryRow();
                    tmpJob.results.temporaryRow = null;
                }
                for (int j = 0; j < this.aggregates.Length; j++)
                    this.aggregates[j].Apply(in row, tmpJob.aggResults[j], resPosition);
            } else
            {
                // If it runs in single thread. No need to merge the results.
                if (this.matcherJobs.Length > 1) 
                { 
                    foreach (var item in tmpJob.groups)
                    {
                        var keyFull = new GroupDictKeyFull(item.Key.hash, tmpJob.results[item.Key.position]);
                        var buckets = this.globalGroups.GetOrAdd(keyFull, tmpJob.spareBuckets);
                        if (tmpJob.spareBuckets != null && object.ReferenceEquals(tmpJob.spareBuckets, buckets))
                            tmpJob.spareBuckets = AggregateBucketResult.CreateBucketResults(this.aggregates);
                        for (int j = 0; j < this.aggregates.Length; j++)
                            this.aggregates[j].MergeThreadSafe(buckets[j], tmpJob.aggResults[j], item.Value);
                    }
                    this.matcherJobs[matcherID] = null;
                }
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            resTable = null;
            if (this.matcherJobs.Length > 1) groupByResults = new ConDictGroupDictKeyFullBucket(this.globalGroups, null);
            else groupByResults = new GroupByResultsList(this.matcherJobs[0].groups, this.matcherJobs[0].aggResults, this.matcherJobs[0].results);
        }

        private class Job
        {
            public TableResults results;
            public Dictionary<GroupDictKey, int> groups;
            public AggregateListResults[] aggResults;
            public RowHasher hasher;
            public AggregateBucketResult[] spareBuckets;

            public Job(Aggregate[] aggregates, int columnCount, RowEqualityComparerGroupKey comparer, RowHasher hasher, AggregateBucketResult[] spareBuckets)
            {
                this.results = new TableResults(columnCount);
                comparer.Results = this.results;
                this.groups = new Dictionary<GroupDictKey, int>(comparer);
                this.hasher = hasher;
                this.spareBuckets = spareBuckets;
                this.aggResults = AggregateListResults.CreateListResults(aggregates);
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
