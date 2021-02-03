using System.Collections.Generic;

namespace QueryEngine 
{
    /// <summary>
    /// Represents a result processor where there are used aggregate functions in the query intput 
    /// but no group by is set.
    /// In that case result of the matchers doesnt have to be stored at all.
    /// The aggregate results will be stored for each matcher in it is separate slot.
    /// And when the matcher finishes it is result will be merged onto the field finalResults.
    /// Thus, when all thread finished the final results will be stored in this field.
    /// </summary>
    internal class SingleGroupResultProcessorHalfStreamed : GroupResultProcessor
    {
        /// <summary>
        /// Each indes array[i] represents one matcher result storage.
        /// The index also represents matcherID.
        /// </summary>
        private AggregateBucketResult[][] matcherResults;
        private int[] numberOfMatchedElements;
        /// <summary>
        /// When matcher finishes, the results are merged onto this property.
        /// </summary>
        private AggregateBucketResult[] finalResults;
        private bool ContainsNonAstrix;

        public SingleGroupResultProcessorHalfStreamed(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper helper, int columnCount): base(expressionInfo, helper, columnCount)
        {
            this.matcherResults = new AggregateBucketResult[helper.ThreadCount][];
            this.numberOfMatchedElements = new int[helper.ThreadCount];
            this.finalResults = AggregateBucketResult.CreateBucketResults(this.aggregates);

            for (int i = 0; i < this.matcherResults.Length; i++)
                this.matcherResults[i] = AggregateBucketResult.CreateBucketResults(this.aggregates);
            for (int i = 0; i < this.aggregates.Length; i++)
                if (!this.aggregates[i].IsAstCount) this.ContainsNonAstrix = true;
        }

        /// <summary>
        /// If the given result is not null, the aggregates for the calling matcher are computed.
        /// If the given result is null, the aggregates are merged onto the field finalResults.
        /// The result == null means that the mather finished it is search.
        /// </summary>
        public override void Process(int matcherID, Element[] result)
        {
            var tmpRes = this.matcherResults[matcherID];
            if (result != null)
            {
                this.numberOfMatchedElements[matcherID]++;
                if (this.ContainsNonAstrix)
                {
                    for (int i = 0; i < this.aggregates.Length; i++)
                    {
                        if (!this.aggregates[i].IsAstCount) this.aggregates[i].Apply(result, tmpRes[i]);
                        else continue;
                    }
                }
            } else
            {
                for (int i = 0; i < this.aggregates.Length; i++)
                {
                    if (!this.aggregates[i].IsAstCount) this.aggregates[i].MergeThreadSafe(this.finalResults[i], tmpRes[i]);
                    else ((Count)this.aggregates[i]).IncByThreadSafe(this.numberOfMatchedElements[matcherID], finalResults[i]);
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
