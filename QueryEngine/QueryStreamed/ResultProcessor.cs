using System;

namespace QueryEngine {

    /// <summary>
    /// The base class for every result processor. 
    /// In the streamed version, instead of using QueryObjects as the execution chain. The result processors are
    /// passed to the matchers of the MatchObject, this enables to process the found result immediately.
    /// </summary>
    internal abstract class ResultProcessor
    {
        protected ResultProcessor next;

        public abstract void Process(int matcherID, Element[] result);

        public void Forward(int matcherID, Element[] result)
        {
            this.next.Process(matcherID, result);
        }

        public abstract void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults);
        
        public void AddToEnd(ResultProcessor nextProc)
        {
            if (nextProc == null)
                throw new ArgumentNullException($"{this.GetType()}, passed result processor is null.");
            else if (this.next != null) this.next.AddToEnd(nextProc);
            else this.next = nextProc;
        }
    }
}
