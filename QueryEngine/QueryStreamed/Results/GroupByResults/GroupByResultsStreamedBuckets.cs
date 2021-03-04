using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;

namespace QueryEngine
{

    /// <summary>
    /// Class representing group by results of the streamed group by.
    /// The derived classes differ only in the dictionary used to store the final results.
    /// </summary>
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
            if (groups == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");


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
            if (groups == null)
                throw new ArgumentNullException($"{this.GetType()}, trying to assign null to a constructor.");

            this.groups = groups;
        }

        public override IEnumerator<AggregateBucketResult[]> GetEnumerator()
        {
            foreach (var item in groups)
                yield return item.Key;
        }
    }
}
