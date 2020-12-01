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
    /// <summary>
    /// The class represents a return value from the groupers that use Array storage for aggregate values.
    /// It contains a proxy class that is used for iteration of the groups.
    /// It enables to access aggregated values through a generic method.
    /// </summary>
    internal class GroupByResultsArray : GroupByResults, IEnumerable<GroupByResultsArray.GroupProxyArray>
    {
        protected ConcurrentDictionary<int, int> groups;
        protected List<AggregateArrayResults> aggregateResults;

        public GroupByResultsArray(ConcurrentDictionary<int, int> groups, List<AggregateArrayResults> aggregateResults, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
            this.aggregateResults = aggregateResults;
        }

        public IEnumerator<GroupByResultsArray.GroupProxyArray> GetEnumerator()
        {
            foreach (var item in groups)
            {
                yield return new GroupByResultsArray.GroupProxyArray(this.resTable[item.Key], item.Value, this.aggregateResults);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public readonly struct GroupProxyArray
        {
            public readonly TableResults.RowProxy groupRepresentant;
            /// <summary>
            /// An position of "this" group's aggregate results in the List storage. 
            /// </summary>
            public readonly int index;
            private readonly List<AggregateArrayResults> aggregatesResults;

            public GroupProxyArray(TableResults.RowProxy groupRepresentant, int index, List<AggregateArrayResults> aggregatesResults)
            {
                this.groupRepresentant = groupRepresentant;
                this.index = index;
                this.aggregatesResults = aggregatesResults;
            }

            public T GetValue<T>(int aggregatePos)
            {
                return ((IGetFinal<T>)this.aggregatesResults[aggregatePos]).GetFinal(this.index);
            }
        }
    }
}
