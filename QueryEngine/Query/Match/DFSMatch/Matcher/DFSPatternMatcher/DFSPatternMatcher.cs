using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// This version of the matcher stores results directly into the Lists provided by the MatchResults class.
    /// </summary>
    internal class DFSPatternMatcher : DFSPatternMatcherBase
    {
        private MatchInternalFixedResults.MatcherFixedResults results;

        /// <summary>
        /// Sets results storage for the matcher instance. 
        /// Everything else is done in the base constructor.
        /// </summary>
        /// <param name="pat"> The pattern to find.</param>
        /// <param name="gr"> The graph to search. </param>
        /// <param name="res"> The object to store found results. </param>
        public DFSPatternMatcher(IDFSPattern pat, Graph gr, MatchInternalFixedResults.MatcherFixedResults res) : base(pat, gr)
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
