using System;

namespace QueryEngine
{
    /// <summary>
    /// The class represents a group by results.
    /// Each derived class must encompass enumerator and a struct that will be used as a way to
    /// access individual results.
    /// </summary>
    internal abstract class GroupByResults
    {
        public int Count;
        protected ITableResults resTable;
    
        protected GroupByResults(int count, ITableResults resTable)
        {
            if (resTable == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

            this.Count = count;
            this.resTable = resTable;
        }
    }
}
