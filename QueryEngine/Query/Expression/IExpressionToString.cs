namespace QueryEngine
{
    /// <summary>
    /// An interface used inside select clause for evaluating expressions and retrieving their values as string.
    /// The interface contains all the valid inputs to the used expressions.
    /// </summary>
    interface IExpressionToString
    {
        string GetValueAsString(in TableResults.RowProxy elements);
        string GetValueAsString(in GroupByResultsList.GroupProxyList group);
        string GetValueAsString(in GroupByResultsBucket.GroupProxyBucket group);
        string GetValueAsString(in GroupByResultsArray.GroupProxyArray group);
        string GetValueAsString(in AggregateBucketResult[] group);
    }
}
