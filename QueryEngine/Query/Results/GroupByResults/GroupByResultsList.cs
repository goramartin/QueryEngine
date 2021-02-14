using System.Collections;
using System.Collections.Generic;
using System;

namespace QueryEngine
{
    /// <summary>
    /// The class represents a return value from the groupers that use List storage for aggregate values.
    /// It contains a proxy class that is used for iteration of the groups.
    /// It enables to access aggregated values through a generic method.
    /// </summary>
    internal class GroupByResultsList : GroupByResults, IEnumerable<GroupByResultsList.GroupProxyList>
    {
        protected Dictionary<GroupDictKey, int> groups;
        protected AggregateListResults[] aggregateResults;

        public GroupByResultsList(Dictionary<GroupDictKey, int> groups, AggregateListResults[] aggregateResults, ITableResults resTable) : base(groups.Count, resTable)
        {
            if (groups == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

            this.groups = groups;
            this.aggregateResults = aggregateResults;
        }

        public IEnumerator<GroupByResultsList.GroupProxyList> GetEnumerator()
        {
            foreach (var item in groups)
            {
                yield return new GroupByResultsList.GroupProxyList(this.resTable[item.Key.position], item.Value, this.aggregateResults);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public readonly struct GroupProxyList
        {
            public readonly TableResults.RowProxy groupRepresentant;
            /// <summary>
            /// An position of "this" group's aggregate results in the List storage. 
            /// </summary>
            public readonly int index;
            private readonly AggregateListResults[] aggregatesResults;

            public GroupProxyList(TableResults.RowProxy groupRepresentant, int index, AggregateListResults[] aggregatesResults)
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
