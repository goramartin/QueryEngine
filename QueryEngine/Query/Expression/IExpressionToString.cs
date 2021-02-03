﻿namespace QueryEngine
{
    interface IExpressionToString
    {
        string GetValueAsString(in TableResults.RowProxy elements);
        string GetValueAsString(in GroupByResultsList.GroupProxyList group);
        string GetValueAsString(in GroupByResultsBucket.GroupProxyBucket group);
        string GetValueAsString(in GroupByResultsArray.GroupProxyArray group);
        string GetValueAsString(in AggregateBucketResult[] group);
    }
}
