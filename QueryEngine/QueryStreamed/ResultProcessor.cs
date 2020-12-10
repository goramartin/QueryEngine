using System;

namespace QueryEngine {

    internal abstract class ResultProcessor
    {
        protected ResultProcessor next;

        public abstract void Process(Element[] result);

        public void Forward(Element[] result)
        {
            this.next.Process(result);
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
