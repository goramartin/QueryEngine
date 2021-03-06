﻿namespace QueryEngine
{
    /// <summary>
    /// A base interface for all matchers.
    /// The returning of the results should be handled separately. (e.g. via passing a storage class to match factory).
    /// </summary>
    internal interface IPatternMatcher
    {
        void Search();
        void SetStoringResults(bool storeResults);
    }

}
