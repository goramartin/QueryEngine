﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
    /// <summary>
    /// Represents a result processor where there are used aggregate function in the query intput 
    /// but no group by is set.
    /// In that case result of the matchers doesnt have to be stored at all.
    /// The aggregate results will be stored for each matcher in it is separate slot.
    /// And when the matcher finishes the field finalResults will be set with it is results.
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
        /// The property will be set by the first matcher that finishes.
        /// </summary>
        private AggregateBucketResult[] finalResults;
        private bool ContainsNonAstrix;

        public SingleGroupResultProcessorHalfStreamed(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper): base(aggs, hashes, helper)
        {
            this.matcherResults = new AggregateBucketResult[helper.ThreadCount][];
            this.numberOfMatchedElements = new int[helper.ThreadCount];
            this.finalResults = AggregateBucketResult.CreateBucketResults(this.aggregates);

            for (int i = 0; i < this.matcherResults.Length; i++)
                this.matcherResults[i] = AggregateBucketResult.CreateBucketResults(this.aggregates);
            for (int i = 0; i < this.aggregates.Count; i++)
                if (!this.aggregates[i].IsAstCount) this.ContainsNonAstrix = true;
        }


        public override void Process(int matcherID, Element[] result)
        {
            var tmpRes = this.matcherResults[matcherID];
            if (result != null)
            {
                this.numberOfMatchedElements[matcherID]++;
                if (this.ContainsNonAstrix)
                {
                    for (int i = 0; i < this.aggregates.Count; i++)
                        if (!this.aggregates[i].IsAstCount) this.aggregates[i].Apply(result, tmpRes[i]);
                        else continue;
                }
            } else
            {
                for (int i = 0; i < this.aggregates.Count; i++)
                    if (!this.aggregates[i].IsAstCount) this.aggregates[i].MergeThreadSafe(this.finalResults[i], tmpRes[i]);
                    else ((Count)this.aggregates[i]).IncByThreadSafe(this.numberOfMatchedElements[matcherID], finalResults[i]);
            }
        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            var tmpDict = new Dictionary<GroupDictKey, AggregateBucketResult[]>();
            tmpDict.Add(new GroupDictKey(0, 0), this.finalResults);
            resTable = null;
            groupByResults = new GroupByResultsBucket(tmpDict, null, null, new TableResults());
        }
    }
}
