using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Base interface for all matchers.
    /// The returning of the results should be handled separately. (e.g. via passing a storage class to match factory).
    /// </summary>
    internal interface IPatternMatcher
    {
        void Search();
        void SetStoringResults(bool storeResults);
    }

}
