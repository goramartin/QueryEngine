using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    internal abstract class GroupByResults
    {
        public int Count;
        protected ITableResults resTable;
    
        protected GroupByResults(int count, ITableResults resTable)
        {
            this.Count = count;
            this.resTable = resTable;
        }
    }
}
