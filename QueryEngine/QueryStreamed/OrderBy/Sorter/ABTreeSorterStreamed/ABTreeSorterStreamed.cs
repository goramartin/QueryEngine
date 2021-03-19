using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a full streamed order by.
    /// Note that single thread version of the full streamed order by is considered to be the same as
    /// the single thread solution of the half streamed order by since it maintains the sorted order for the 
    /// entire set of immediate results. 
    /// The class contains an array of objects (ab tree, table results), each object represents a 
    /// particular range from the universum of the first key that is used to sort the results.
    /// The key is computed for each incoming result and a hasher class is used to determine the correct range 
    /// the result belongs to. After the determination is done, the thread locks the object representing the range
    /// and inserts it into the table and into the ab tree.
    /// The generics of this class is to manipulate more easily with the type of the first key.
    /// </summary>
    /// <typeparam name="T"> A type of the first key that it sorts with. </typeparam>
    internal abstract class ABTreeStreamedSorter<T> : OrderByResultProcessor
    {
        /// <summary>
        /// The universum of the first key split into ranges.
        /// </summary>
        protected RangeBucket[] rangeBuckets;
        /// <summary>
        /// Comparers used inside the AB trees, so that after computing the first key.
        /// The comparer upon insert would otherwise compute it again.
        /// </summary>
        protected ExpressionComparer<T>[] firstKeyComparers;

        /// <summary>
        /// Expression to compute the first key.
        /// </summary>
        protected ExpressionHolder firstKeyExpressionHolder;
        protected ExpressionReturnValue<T> firstKeyExpression;

        /// <summary>
        /// Hasher that determines the correct bucket of the given value.
        /// </summary>
        protected TypeRangeHasher<T> firstKeyHasher;
        
        public ABTreeStreamedSorter(ExpressionComparer[] comparers, IOrderByExecutionHelper executionHelper, int columnCount, int[] usedVars, bool allowDup): base(comparers, executionHelper, columnCount, usedVars)
        {
            this.firstKeyHasher = (TypeRangeHasher<T>)TypeRangeHasher.Factory(this.executionHelper.ThreadCount, typeof(T));
            this.firstKeyExpressionHolder = this.comparers[0].GetExpressionHolder();
            this.firstKeyExpression = (ExpressionReturnValue<T>)this.firstKeyExpressionHolder.Expr;
            
            this.rangeBuckets = new RangeBucket[this.firstKeyHasher.BucketCount];
            this.firstKeyComparers = new ExpressionComparer<T>[this.rangeBuckets.Length];
            for (int i = 0; i < this.rangeBuckets.Length; i++)
            {
                var results = new TableResults(this.ColumnCount, this.executionHelper.FixedArraySize, this.usedVars);
                var tmpRowComparer = RowComparer.Factory(this.comparers, true);
                this.firstKeyComparers[i] = (ExpressionComparer<T>)tmpRowComparer.comparers[0];
                this.rangeBuckets[i] = CreateBucket(new IndexToRowProxyComparer(tmpRowComparer, results, allowDup), results);
            }
        }
        protected abstract RangeBucket CreateBucket(IComparer<int> comparer, ITableResults resTable);

        public override void Process(int matcherID, Element[] result)
        {
            if (result == null) return;
            
            // Compute the correct placement (range) of the computed value.
            // Else it will be placed into the first bucket since the value is null.
            int bucketIndex = 0;
            if (this.executionHelper.InParallel)
            {
                bool evalSuccess = this.firstKeyExpression.TryEvaluate(result, out T resValue);
                if (evalSuccess)
                    bucketIndex = this.firstKeyHasher.Hash(resValue);

                var bucket = this.rangeBuckets[bucketIndex];
                var comp = this.firstKeyComparers[bucketIndex];
                lock (bucket)
                {
                    bucket.resTable.StoreRow(result);

                    // Set the Y cache since the internal impl. uses the right param when comparing.
                    comp.SetYCache(evalSuccess, bucket.resTable.RowCount - 1, resValue);
                    bucket.tree.Insert(bucket.resTable.RowCount - 1);
                }
            } else
            {
                var bucket = this.rangeBuckets[bucketIndex];
                bucket.resTable.StoreRow(result);
                bucket.tree.Insert(bucket.resTable.RowCount - 1);
            }
        }

        /// <summary>
        /// A class that represents a certain range of results.
        /// What range it represents is confined in the enclosing class.
        /// </summary>
        protected class RangeBucket
        {
            public IABTree<int> tree;
            public ITableResults resTable;
        }

    }
}
