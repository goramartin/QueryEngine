using System;

namespace QueryEngine
{
    /// <summary>
    /// This version of the matcher stores results directly into the Lists provided by the MatchResults class.
    /// </summary>
    internal class DFSPatternMatcher : DFSPatternMatcherBase
    {
        private MatchFixedResults.MatcherFixedResultsInternal results;

        /// <summary>
        /// Sets results storage for the matcher instance. 
        /// Everything else is done in the base constructor.
        /// </summary>
        /// <param name="pat"> A pattern to find.</param>
        /// <param name="gr"> A graph to search. </param>
        /// <param name="res"> An object to store found results. </param>
        public DFSPatternMatcher(IDFSPattern pat, Graph gr, MatchFixedResults.MatcherFixedResultsInternal res) : base(pat, gr)
        {
            if (res == null)
                throw new ArgumentException($"{this.GetType()}, passed null to a constructor.");
            this.results = res;
        }

        protected override void ProccessResult()
        {
            var scope = this.pattern.GetMatchedVariables();
            this.NumberOfMatchedElements++;

            if (this.isStoringResults)
                this.results.AddRow(scope);
        }
    }
}
