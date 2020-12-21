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
    internal abstract class GroupByResultsStreamedBucket : GroupByResults, IEnumerable<AggregateBucketResult[]>
    {
        public GroupByResultsStreamedBucket(int count, ITableResults resTable) : base(count, resTable)
        { }

        public abstract IEnumerator<AggregateBucketResult[]> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

    }

    internal class ConDictStreamedBucket : GroupByResultsStreamedBucket
    {
        protected ConcurrentDictionary<AggregateBucketResult[], AggregateBucketResult[]> groups;

        public ConDictStreamedBucket(ConcurrentDictionary<AggregateBucketResult[], AggregateBucketResult[]> groups, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
        }

        public override IEnumerator<AggregateBucketResult[]> GetEnumerator()
        {
            foreach (var item in groups)
                yield return item.Key;
        }
    }

    internal class DictStreamedBucket : GroupByResultsStreamedBucket
    {
        protected Dictionary<AggregateBucketResult[], AggregateBucketResult[]> groups;


        public DictStreamedBucket(Dictionary<AggregateBucketResult[], AggregateBucketResult[]> groups, ITableResults resTable) : base(groups.Count, resTable)
        {
            this.groups = groups;
        }

        public override IEnumerator<AggregateBucketResult[]> GetEnumerator()
        {
            foreach (var item in groups)
                yield return item.Key;
        }
    }
}
