using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine
{
    internal class SingleGroupResultProcessorStreamed : GroupResultProcessor
    {
        private AggregateBucketResult[] finalResults;
        private int numberOfMatchedElements;
        private int matchersFinished;
        private bool ContainsNonAsterix;

        public SingleGroupResultProcessorStreamed(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper) : base(aggs, hashes, helper)
        {
            this.finalResults = AggregateBucketResult.CreateBucketResults(this.aggregates);
            for (int i = 0; i < this.aggregates.Count; i++)
                if (!this.aggregates[i].IsAstCount) this.ContainsNonAsterix = true;
        }


        public override void Process(int matcherID, Element[] result)
        {
            if (result != null)
            {
                Interlocked.Increment(ref this.numberOfMatchedElements);
                if (this.ContainsNonAsterix)
                {
                    for (int i = 0; i < this.aggregates.Count; i++)
                        if (!this.aggregates[i].IsAstCount) this.aggregates[i].Apply(result, finalResults[i]);
                        else continue;
                }
            } else
            {
                var tmp = Interlocked.Increment(ref this.matchersFinished);
                // The last thread stores the number of matched elements.
                if (tmp == this.ThreadCount)
                {
                    for (int i = 0; i < this.aggregates.Count; i++)
                        if (this.aggregates[i].IsAstCount) ((Count)this.aggregates[i]).IncBy(this.numberOfMatchedElements, this.finalResults[i]);
                }
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
