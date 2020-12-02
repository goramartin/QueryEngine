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
    /// The class represents a return value from the groupers that use Bucket storage for aggregate values.
    /// It contains a proxy class that is used for iteration of the groups.
    /// It enables to access aggregated values through a generic method.
    /// </summary>
    internal class GroupByResultsBucket : GroupByResults, IEnumerable<GroupByResultsBucket.GroupProxyBucket>
    {
        // To do make divided.
        protected Dictionary<GroupDictKey, AggregateBucketResult[]> groups;
        protected ConcurrentDictionary<int, AggregateBucketResult[]> groupsCon;
        protected ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> groupsConGroupKey;

        public GroupByResultsBucket(Dictionary<GroupDictKey, AggregateBucketResult[]> groups, ConcurrentDictionary<int, AggregateBucketResult[]> groupsCon, ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> groupsConGroupKey, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
            this.groupsCon = groupsCon;
            this.groupsConGroupKey = groupsConGroupKey;
        }

        public virtual IEnumerator<GroupByResultsBucket.GroupProxyBucket> GetEnumerator()
        {
            if (this.groups != null)
            {
                foreach (var item in groups)
                    yield return new GroupByResultsBucket.GroupProxyBucket(this.resTable[item.Key.position], item.Value);
            } else if (groupsCon != null)
            {
                foreach (var item in groupsCon)
                    yield return new GroupByResultsBucket.GroupProxyBucket(this.resTable[item.Key], item.Value);
            } else
            {
                foreach (var item in groupsConGroupKey)
                    yield return new GroupByResultsBucket.GroupProxyBucket(this.resTable[item.Key.position], item.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public readonly struct GroupProxyBucket
        {
            public readonly TableResults.RowProxy groupRepresentant;
            private readonly AggregateBucketResult[] aggregatesResults;

            public GroupProxyBucket(TableResults.RowProxy groupRepresentant, AggregateBucketResult[] aggregatesResults)
            {
                this.groupRepresentant = groupRepresentant;
                this.aggregatesResults = aggregatesResults;
            }

            public T GetValue<T>(int aggregatePos)
            {
                return ((IGetFinal<T>)this.aggregatesResults[aggregatePos]).GetFinal(0);
            }
        }
    }

}
