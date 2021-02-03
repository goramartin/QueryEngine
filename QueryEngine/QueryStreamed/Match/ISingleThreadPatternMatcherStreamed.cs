namespace QueryEngine
{

    /// <summary>
    /// This interface is added only to ensure uniformity of interfaces 
    /// during the streamed pattern matching.
    /// </summary>
    interface ISingleThreadPatternMatcherStreamed: IPatternMatcherStreamed, ISingleThreadPatternMatcher
    { }
}
