﻿namespace QueryEngine
{
    /// <summary>
    /// A base interface for printers. It defines the neccessary functions, that will be handle by the printer.
    /// </summary>
    interface IRowPrinter
    {
        void PrintRow(in TableResults.RowProxy elements);
        void PrintRow(in GroupByResultsList.GroupProxyList group);
        void PrintRow(in GroupByResultsBucket.GroupProxyBucket group);
        void PrintRow(in GroupByResultsArray.GroupProxyArray group);
        void PrintRow(in AggregateBucketResult[] group);
    }
}
