using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    interface IRowPrinter
    {
        void PrintRow(in TableResults.RowProxy elements);
        void PrintRow(in GroupByResultsList.GroupProxyList group);
        void PrintRow(in GroupByResultsBucket.GroupProxyBucket group);
        void PrintRow(in GroupByResultsArray.GroupProxyArray group);
        void PrintRow(in AggregateBucketResult[] group);
    }
}
