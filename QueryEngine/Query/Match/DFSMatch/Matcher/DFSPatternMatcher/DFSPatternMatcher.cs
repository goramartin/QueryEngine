using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// The reason the class was split is that the streamed version of the matcher will use 
    /// the same search algorithm but instead of storing results in the lists, it will forward the results
    /// from the ProcessResult to further processing.
    /// </summary>
    internal class DFSPatternMatcher : DFSPatternMatcherBase
    {
        private List<Element>[] results;

        /// <summary>
        /// Sets results storage for the matcher instance. 
        /// Everything else is done in the base constructor.
        /// </summary>
        /// <param name="pat"> The pattern to find.</param>
        /// <param name="gr"> The graph to search. </param>
        /// <param name="res"> The object to store found results. </param>
        public DFSPatternMatcher(IDFSPattern pat, Graph gr, List<Element>[] res) : base(pat, gr)
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
            {
                for (int i = 0; i < this.results.Length; i++)
                    this.results[i].Add(scope[i]);
            }
        }
    }
}
