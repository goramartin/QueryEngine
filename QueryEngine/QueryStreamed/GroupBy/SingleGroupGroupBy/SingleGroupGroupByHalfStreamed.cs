using System.Collections.Generic;
using System;
using System.Threading;

namespace QueryEngine 
{
    /// <summary>
    /// Represents a result processor where there are used aggregate functions in the query intput 
    /// but no group by is set.
    /// In that case result of the matchers doesnt have to be stored at all.
    /// The aggregate results will be stored for each matcher in it is separate slot.
    /// And when the matcher finishes it is result will be merged onto the field finalResults.
    /// Thus, when all thread finished the final results will be stored in this field.
    /// 
    /// The aggregate results are divided into aggregates that contain asterix and the rest.
    /// This is done in order to omit call on the Count aggregate.
    /// </summary>
    internal class SingleGroupGroupByHalfStreamed : GroupByResultProcessor
    {
        private Aggregate[] nonAsterixAggregates;
        private int[] numberOfMatchedElements;
        
        /// <summary>
        /// Each indes array[i] represents one matcher result storage.
        /// The index also represents matcherID.
        /// </summary>
        private AggregateBucketResult[][] matcherNonAsterixResults;
        /// <summary>
        /// When matcher finishes, the results are merged onto this property.
        /// </summary>
        private AggregateBucketResult[] finalResults;
        /// <summary>
        /// Contains final results from "finalResults" field without count(*).
        /// Note that updating this field updates also the appropriate field in the "finalResuls".
        /// </summary>
        private AggregateBucketResult[] finalNonAsterixResults;
        private bool containsAst;

        public SingleGroupGroupByHalfStreamed(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper helper, int columnCount, int[] usedVars): base(expressionInfo, helper, columnCount, usedVars)
        {
            this.matcherNonAsterixResults = new AggregateBucketResult[this.executionHelper.ThreadCount][];
            this.finalResults = AggregateBucketResult.CreateBucketResults(this.aggregates);
            Aggregate.ExtractNonAstAggsAndResults(this.aggregates, this.finalResults, out nonAsterixAggregates, out finalNonAsterixResults);
            if (this.finalResults.Length != this.finalNonAsterixResults.Length) this.containsAst = true;

            this.numberOfMatchedElements = new int[this.executionHelper.ThreadCount];
            for (int i = 0; i < this.matcherNonAsterixResults.Length; i++)
                this.matcherNonAsterixResults[i] = AggregateBucketResult.CreateBucketResults(this.nonAsterixAggregates);
        }

        public override void Process(int matcherID, Element[] result)
        {
            var matcherResults = this.matcherNonAsterixResults[matcherID];
            if (result != null)
            {
                for (int i = 0; i < this.nonAsterixAggregates.Length; i++)
                    this.nonAsterixAggregates[i].Apply(result, matcherResults[i]);
                if (this.containsAst) this.numberOfMatchedElements[matcherID]++;
                else { }
            } else
            {
                // Merge into non ast results.
                for (int i = 0; i < this.nonAsterixAggregates.Length; i++)
                {
                    if (this.executionHelper.InParallel) 
                        this.nonAsterixAggregates[i].MergeThreadSafe(this.finalNonAsterixResults[i], matcherResults[i]);
                    else this.nonAsterixAggregates[i].Merge(this.finalNonAsterixResults[i], matcherResults[i]);
                }
                    
                if (this.containsAst)
                {
                    // Merge the number of matched elements.
                    for (int i = 0; i < this.aggregates.Length; i++)
                        if (this.aggregates[i].IsAstCount)
                        {
                            if (this.executionHelper.InParallel) ((Count<int>)this.aggregates[i]).IncByThreadSafe(this.numberOfMatchedElements[matcherID], finalResults[i]);
                            else ((Count<int>)this.aggregates[i]).IncBy(this.numberOfMatchedElements[matcherID], finalResults[i]);
                        }
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
