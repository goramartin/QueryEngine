using System;

namespace QueryEngine {

    /// <summary>
    /// The base class for every result processor. 
    /// In the streamed version, instead of using QueryObjects as the execution chain. The result processors are
    /// passed to the matchers of the MatchObject, this enables to process the found result immediately.
    /// Because the architecture provides only streamed versions of the order by and group by, thus the class also 
    /// provides a method to retrieve aggregated results.
    /// 
    /// The idea is that result processors can be chained, in order to provide processing based on the computed query.
    /// For example, if Where clause was used, there could be a result processor that would implement an alogirthm for 
    /// where clauses and forward results that are coherent with the defined expressions in the Where clause.
    /// </summary>
    internal abstract class ResultProcessor
    {
        protected ResultProcessor next;

        /// <summary>
        /// It is expected that the derived classes implement their own processing methods.
        /// The general rule is if result == null, it is the signal that there will be no more results.
        /// </summary>
        public abstract void Process(int matcherID, Element[] result);
        public abstract void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults);
        

        public void AddToEnd(ResultProcessor nextProc)
        {
            if (nextProc == null)
                throw new ArgumentNullException($"{this.GetType()}, passed result processor is null.");
            else if (this.next != null) this.next.AddToEnd(nextProc);
            else this.next = nextProc;
        }
        public void Forward(int matcherID, Element[] result)
        {
            this.next.Process(matcherID, result);
        }
    }
}
