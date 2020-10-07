/*! \file
This file includes interfaces to all matchers.
Matchers are used during matching algorithm. They search the graph for user inpputed match expression.
Returning results from the matching should be handled independently.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base interface for all matchers.
    /// The returning of the results should be done separately from the searching.
    /// </summary>
    internal interface IPatternMatcher
    {
        void Search();
    }

    /// <summary>
    /// Single threaded matcher is used by a parallel matcher.
    /// Represents one thread with a matcher.
    /// </summary>
    internal interface ISingleThreadMatcher : IPatternMatcher
    {
        void SetStartingVerticesIndeces(int start, int end);
        void SetStoringResults(bool storeResults);
        int GetNumberOfMatchedElements();
    }

}
