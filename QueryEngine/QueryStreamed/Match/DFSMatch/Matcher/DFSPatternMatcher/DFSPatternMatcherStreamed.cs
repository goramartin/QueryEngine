using System;

namespace QueryEngine
{
    /// <summary>
    /// The matcher will instead of storing results into a table will pass the result
    /// for further processing. Otherwise it works the same as the normal one.
    /// </summary>
    internal class DFSPatternMatcherStreamed : DFSPatternMatcherBase, ISingleThreadPatternMatcherStreamed
    {
        private ResultProcessor resultProcessor;
        private int matcherID;

        /// <summary>
        /// Sets results storage for the matcher instance. 
        /// Everything else is done in the base constructor.
        /// </summary>
        /// <param name="pat"> The pattern to find.</param>
        /// <param name="gr"> The graph to search. </param>
        /// <param name="mID"> ID of the matcher. Will be used as a resultProcessor argument.</param>
        public DFSPatternMatcherStreamed(IDFSPattern pat, Graph gr, int mID) : base(pat, gr)
        {
            if (mID < 0)
                throw new ArgumentException($"{this.GetType()}, matcherId cannot be < 0");
            else this.matcherID = mID;
        }

        public void PassResultProcessor(ResultProcessor resultProcessor)
        {
            if (resultProcessor == null)
                throw new ArgumentNullException($"{this.GetType()}, passed a null as a processor.");
            else this.resultProcessor = resultProcessor;
        }

        /// <summary>
        /// Note that this method will be called only if there was set group by 
        /// or order by, thus, there is no need to check the flag for storing the results.
        /// </summary>
        protected override void ProccessResult()
        {
            var scope = this.pattern.GetMatchedVariables();
            this.resultProcessor.Process(this.matcherID, scope);
        }
    }
}
