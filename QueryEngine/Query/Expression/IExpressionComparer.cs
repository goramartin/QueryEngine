namespace QueryEngine
{
    /// <summary>
    /// An interface used for comparing expression values.
    /// The returned values are the same as in the native .CompareTo methods.
    /// The interface contains all the valid inputs to the used expressions. 
    /// </summary>
    internal interface IExpressionComparer
    {
        int Compare(in TableResults.RowProxy x, in TableResults.RowProxy y);
    }

}
