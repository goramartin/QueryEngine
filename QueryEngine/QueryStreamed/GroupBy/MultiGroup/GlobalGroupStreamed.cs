﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine
{
    internal class GlobalGroupStreamed : GroupResultProcessor
    {
        ConcurrentDictionary<AggregateBucketResult[], AggregateBucketResult[]> globalGroups;

        public GlobalGroupStreamed(List<Aggregate> aggs, List<ExpressionHolder> hashes, IGroupByExecutionHelper helper, int columnCount) : base(aggs, hashes, helper, columnCount)
        {
            
            // to do





        }

        public override void Process(int matcherID, Element[] result)
        {
            if (result == null) return;
            else if (this.InParallel) ProcessParallel(matcherID, result);
            else ProcessSingleThread(matcherID, result);
        }


        private void ProcessSingleThread(int matcherID, Element[] result)
        {





        }

        private void ProcessParallel(int matcherID, Element[] result)
        {




        }

        public override void RetrieveResults(out ITableResults resTable, out GroupByResults groupByResults)
        {
            resTable = new TableResults();

            // to do
        }


        private class MatcherJobST
        {
            public Dictionary<AggregateBucketResult[], AggregateBucketResult[]> groups;
            // to do


            public MatcherJobST(RowEqualityComparerAggregateBucketResult comparer)
            {
                this.groups = new Dictionary<AggregateBucketResult[], AggregateBucketResult[]>(comparer);
            }
        }

        private class MatcherJobPar
        {
            // to do


        }
    }
}
