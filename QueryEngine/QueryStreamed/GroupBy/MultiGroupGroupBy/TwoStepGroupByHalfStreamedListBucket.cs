using System.Collections.Concurrent;
using System.Collections.Generic;

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
    /// Notice that if it runs in single thread, the mergings do not happen. Thus we can use this class
    /// as a single thread reference solution for the half streamed/streamed version using lists as a local result storage.
    /// </summary>
    internal class TwoStepHalfStreamedListBucket : GroupByResultProcessor
    {
        private GroupJob[] groupJobs;
        private ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]> globalGroups;

        public TwoStepHalfStreamedListBucket(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount, int[] usedVars) : base(expressionInfo, executionHelper, columnCount, usedVars)
        {
            this.groupJobs = new GroupJob[this.executionHelper.ThreadCount];
           
            // Create initial job, comps and hashers
            this.CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers);
            var firstComp = RowEqualityComparerGroupKey.Factory(null, comparers, true);
            var firstHasher = new RowHasher(hashers);
            firstHasher.SetCache(firstComp.comparers);
            
            this.groupJobs[0] = new GroupJob(this.aggregates, firstComp, firstHasher, AggregateBucketResult.CreateBucketResults(this.aggregates), new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize, this.usedVars));
            for (int i = 1; i < this.executionHelper.ThreadCount; i++)
            {
                CloneHasherAndComparer(firstComp, firstHasher, out RowEqualityComparerGroupKey newComp, out RowHasher newHasher);
                groupJobs[i] = new GroupJob(this.aggregates, newComp, newHasher, AggregateBucketResult.CreateBucketResults(this.aggregates), new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize, this.usedVars));
            }

            this.globalGroups = new ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]>( RowEqualityComparerGroupDickKeyFull.Factory(comparers, false));
        }

        public override void Process(int matcherID, Element[] result)
        {
            var job = this.groupJobs[matcherID];
            if (result != null)
            {
                // Create a temporary row.
                job.resTable.temporaryRow = result;
                int rowPosition = job.resTable.RowCount;
                TableResults.RowProxy row = job.resTable[rowPosition];
                var key = new GroupDictKey(job.hasher.Hash(in row), rowPosition); // It's a struct.

                if (!job.groups.TryGetValue(key, out int resPosition))
                {
                    resPosition = job.groups.Count;
                    job.groups.Add(key, resPosition);
                    // Store the temporary row in the table. This causes copying of the row to the actual lists of table.
                    // While the position of the stored row proxy remains the same, next time someone tries to access it,
                    // it returns the elements from the actual table and not the temporary row.
                    job.resTable.StoreTemporaryRow();
                    job.resTable.temporaryRow = null;
                }
                for (int j = 0; j < this.aggregates.Length; j++)
                    this.aggregates[j].Apply(in row, job.aggResults[j], resPosition);
            } else
            {
                // If it runs in single thread. No need to merge the results.
                if (this.groupJobs.Length > 1) 
                {
                    MergeResults(job, matcherID);
                }
            }
        }

        /// <summary>
        /// Called only if the grouping runs in paralel.
        /// Merges local group results into the global results.
        /// </summary>
        private void MergeResults(GroupJob job, int matcherID)
        {
            foreach (var item in job.groups)
            {
                var keyFull = new GroupDictKeyFull(item.Key.hash, job.resTable[item.Key.position]);
                var buckets = this.globalGroups.GetOrAdd(keyFull, job.spareBuckets);
                if (job.spareBuckets != null && object.ReferenceEquals(job.spareBuckets, buckets))
                    job.spareBuckets = AggregateBucketResult.CreateBucketResults(this.aggregates);
                for (int j = 0; j < this.aggregates.Length; j++)
                    this.aggregates[j].MergeThreadSafe(buckets[j], job.aggResults[j], item.Value);
            }
            this.groupJobs[matcherID] = null;
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            resTable = null;
            if (this.groupJobs.Length > 1) groupByResults = new ConDictGroupDictKeyFullBucket(this.globalGroups, new TableResults());
            else groupByResults = new GroupByResultsList(this.groupJobs[0].groups, this.groupJobs[0].aggResults, this.groupJobs[0].resTable);
        }

        private class GroupJob
        {
            public ITableResults resTable;
            public Dictionary<GroupDictKey, int> groups;
            public AggregateListResults[] aggResults;
            public RowHasher hasher;
            public AggregateBucketResult[] spareBuckets;

            public GroupJob(Aggregate[] aggregates, RowEqualityComparerGroupKey comparer, RowHasher hasher, AggregateBucketResult[] spareBuckets, ITableResults resTable)
            {
                this.resTable = resTable;
                comparer.resTable = this.resTable;
                this.groups = new Dictionary<GroupDictKey, int>(comparer);
                this.hasher = hasher;
                this.spareBuckets = spareBuckets;
                this.aggResults = AggregateListResults.CreateListResults(aggregates);
            }
        }
    }
}
