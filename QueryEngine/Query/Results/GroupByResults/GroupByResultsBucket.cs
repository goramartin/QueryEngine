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
    internal abstract class GroupByResultsBucket : GroupByResults, IEnumerable<GroupByResultsBucket.GroupProxyBucket>
    {
        // To do make divided.
        public GroupByResultsBucket(int count, ITableResults resTable) : base(count, resTable)
        {}

        public abstract IEnumerator<GroupByResultsBucket.GroupProxyBucket> GetEnumerator();
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


    internal class DictGroupDictKeyBucket: GroupByResultsBucket
    {
        protected Dictionary<GroupDictKey, AggregateBucketResult[]> groups;
        public DictGroupDictKeyBucket(Dictionary<GroupDictKey, AggregateBucketResult[]> groups, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
        }

        public override IEnumerator<GroupProxyBucket> GetEnumerator()
        {
            foreach (var item in groups)
                yield return new GroupByResultsBucket.GroupProxyBucket(this.resTable[item.Key.position], item.Value);
        }
    }

    internal class ConDictGroupByResultsBucket : GroupByResultsBucket
    {
        protected ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> groups;
        public ConDictGroupByResultsBucket(ConcurrentDictionary<GroupDictKey, AggregateBucketResult[]> groups, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
        }

        public override IEnumerator<GroupProxyBucket> GetEnumerator()
        {
            foreach (var item in groups)
                yield return new GroupByResultsBucket.GroupProxyBucket(this.resTable[item.Key.position], item.Value);
        }
    }

    internal class ConDictIntBucket  : GroupByResultsBucket
    {
        protected ConcurrentDictionary<int, AggregateBucketResult[]> groups;
        public ConDictIntBucket(ConcurrentDictionary<int, AggregateBucketResult[]> groups, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
        }

        public override IEnumerator<GroupProxyBucket> GetEnumerator()
        {
            foreach (var item in groups)
                yield return new GroupByResultsBucket.GroupProxyBucket(this.resTable[item.Key], item.Value);
        }
    }

    internal class ConDictGroupDictKeyFullBucket : GroupByResultsBucket
    {
        protected ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]> groups;


        public ConDictGroupDictKeyFullBucket(ConcurrentDictionary<GroupDictKeyFull, AggregateBucketResult[]> groups, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
        }

        public override IEnumerator<GroupProxyBucket> GetEnumerator()
        {
            foreach (var item in groups)
                yield return new GroupByResultsBucket.GroupProxyBucket(item.Key.row, item.Value);
        }
    }

}
