namespace QueryEngine 
{
    /// <summary>
    /// The interface enhances the base interface for a method that will enable to pass
    /// result processor chain to matchers.
    /// </summary>
    interface IPatternMatcherStreamed : IPatternMatcher
    {
        void PassResultProcessor(ResultProcessor resultProcessor);
    }
}
