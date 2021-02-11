using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class represents a full streamed order by.
    /// Note that single thread version of the full streamed order by is considered to be the same as
    /// the single thread solution of the half streamed order by since it maintains the sorted order for the 
    /// entire set of immediate results. 
    /// This class should be used solely in the parallel enviroment.
    /// The class contains an array of objects (ab tree, table results), each object represents a 
    /// particular range from the universum of the first key that is used to sort the results.
    /// The key is computed for each incoming result and a hasher class is used to determine the correct range 
    /// the result belongs to. After the determination is done, the thread locks the object representing the range
    /// and inserts it into the table and into the ab tree.
    /// The generics of this class is to manipulate more easily with the type of the first key.
    /// </summary>
    /// <typeparam name="T"> A type of the first key that it sorts with. </typeparam>
    internal class ABTreeStreamedSorter<T> : OrderByResultProcessor
    {
        /// <summary>
        /// The universum of the first key split into ranges.
        /// </summary>
        private RangeBucket[] rangeBuckets;
        /// <summary>
        /// Comparers used inside the AB trees, so that after computing the first key.
        /// The comparer upon insert would otherwise compute it again.
        /// </summary>
        private ExpressionComparer<T>[] firstKeyComparers;

        /// <summary>
        /// Expression to compute the first key.
        /// </summary>
        private ExpressionHolder firstKeyExpressionHolder;
        private ExpressionReturnValue<T> firstKeyExpression;

        /// <summary>
        /// Hasher that determines the correct range bucket of the given value.
        /// </summary>
        private FirstKeyHasher<T> firstKeyHasher;
        

        public ABTreeStreamedSorter(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo, int columnCount)
            : base(graph, variableMap, executionHelper, orderByNode, exprInfo, columnCount)
        {
            throw new NotImplementedException($"{this.GetType()}");



        }

        public override void Process(int matcherID, Element[] result)
        {
            bool evalSuccess = this.firstKeyExpression.TryEvaluate(result, out T resValue);
            
            // Compute the correct placement (range) of the computed value.
            // Else it will be placed into the first bucket since the value is null.
            int bucketIndex = 0;
            
            if (this.executionHelper.InParallel)
            {
                if (evalSuccess)
                    bucketIndex = this.firstKeyHasher.Hash(resValue);

                lock (this.rangeBuckets[bucketIndex])
                {
                    var bucket = this.rangeBuckets[bucketIndex];
                    bucket.resTable.StoreRow(result);

                    // Set the Y cache since the internal impl. uses the right param when comparing.
                    this.firstKeyComparers[bucketIndex].SetYCache(evalSuccess, bucket.resTable.RowCount - 1, resValue);
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
        private class RangeBucket
        {
            public ABTree<int> tree;
            public ITableResults resTable;

            public RangeBucket(IComparer<int> comparer, ITableResults resTable)
            {
                this.tree = new ABTree<int>(256, comparer);
                this.resTable = resTable;
            }

        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            throw new NotImplementedException();
        }
    }
}
