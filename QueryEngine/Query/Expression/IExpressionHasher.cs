namespace QueryEngine
{
    /// <summary>
    /// An interface used inside group by clause for evaluating expressions and retrieving their hash values.
    /// The interface contains all the valid inputs to the used expressions.
    /// </summary>
    internal interface IExpressionHasher
    {
        int Hash(in TableResults.RowProxy row);
    }
}
