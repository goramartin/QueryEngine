using System.Collections.Generic;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// Represents a result processor where there are used aggregate functions in the query intput 
    /// but no group by is set.
    /// In that case result of the matchers doesnt have to be stored at all.
    /// The aggregate results of every matcher will be stored in the field finalResults (a global field).
    /// The aggregate results are computed in a thread-safe manner.
    /// This simulated full streamed version, where the aggregates in the field finalResults, contain
    /// the newest values.
    /// Notice that trying this algorithm makes sense only in the parallel enviroment.
    /// </summary>
    internal class SingleGroupResultProcessorStreamed : GroupResultProcessor
    {
        private AggregateBucketResult[] finalResults;
        private int numberOfMatchedElements;
        private int matchersFinished;
        private bool ContainsNonAsterix;

        public SingleGroupResultProcessorStreamed(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount) : base(expressionInfo, executionHelper, columnCount)
        {
            this.finalResults = AggregateBucketResult.CreateBucketResults(this.aggregates);
            for (int i = 0; i < this.aggregates.Length; i++)
                if (!this.aggregates[i].IsAstCount) this.ContainsNonAsterix = true;
        }

        /// <summary>
        /// If the given result is not null, the aggregates for the calling matcher are computed.
        /// If the given result is null, the aggregates are merged onto the field finalResults.
        /// The result == null means that the mather finished it is search.
        /// </summary>
        public override void Process(int matcherID, Element[] result)
        {
            if (result != null)
            {
                Interlocked.Increment(ref this.numberOfMatchedElements);
                if (this.ContainsNonAsterix)
                {
                    for (int i = 0; i < this.aggregates.Length; i++)
                        if (!this.aggregates[i].IsAstCount) this.aggregates[i].ApplyThreadSafe(result, finalResults[i]);
                        else continue;
                }
            } else
            {
                // Signal that the matcher has finished.
                var tmp = Interlocked.Increment(ref this.matchersFinished);
                // The last finished matcher stores the number of matched elements.
                if (tmp == this.executionHelper.ThreadCount)
                {
                    for (int i = 0; i < this.aggregates.Length; i++)
                        if (this.aggregates[i].IsAstCount) ((Count)this.aggregates[i]).IncBy(this.numberOfMatchedElements, this.finalResults[i]);
                }
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            var tmpDict = new Dictionary<GroupDictKey, AggregateBucketResult[]>();
            tmpDict.Add(new GroupDictKey(0, 0), this.finalResults);
            resTable = new TableResults();
            groupByResults = new DictGroupDictKeyBucket(tmpDict, resTable);
        }
    }
}
