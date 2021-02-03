namespace QueryEngine
{
    /// <summary>
    /// Single threaded matcher is used by a parallel matcher.
    /// Represents one thread within a matcher.
    /// </summary>
    internal interface ISingleThreadPatternMatcher : IPatternMatcher
    {
        void SetStartingVerticesIndeces(int start, int end);
        int GetNumberOfMatchedElements();
    }
}
