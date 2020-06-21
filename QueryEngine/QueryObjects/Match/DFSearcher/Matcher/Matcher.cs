/*! \file
    
    This file includes interfaces to all dfs matchers.
  
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
    }

    internal interface IParallelMatcher : IPatternMatcher
    {
    }
}
