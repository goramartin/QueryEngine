﻿using System;
using System.Collections.Generic;


namespace QueryEngine
{
    /// <summary>
    /// A base class for every group result processor.
    /// When instantiating the class, the users input query must be parsed before running constructors of any child class.
    /// </summary>
    internal abstract class GroupByResultProcessor : ResultProcessor
    {
        protected Aggregate[] aggregates;
        protected ExpressionHolder[] hashes;
        protected IGroupByExecutionHelper executionHelper;
        /// <summary>
        /// Represents a number of variables defined in the match clause of the query.
        /// </summary>
        protected int ColumnCount { get; }
        protected int[] usedVars;

        protected GroupByResultProcessor(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount, int[] usedVars)
        {
            this.aggregates = expressionInfo.Aggregates.ToArray();
            this.hashes = expressionInfo.GroupByhashExprs.ToArray();
            this.executionHelper = executionHelper;
            this.ColumnCount = columnCount;
            this.usedVars = usedVars;
        }

        /// <summary>
        /// Creates a list of hashers and comparers.
        /// Notice that the number of comparers is equal to the number of hashers.
        /// Also, they must be of the same generic type.
        /// </summary>
        protected void CreateHashersAndComparers(out ExpressionComparer[] comparers, out ExpressionHasher[] hashers)
        {
            comparers = new ExpressionComparer[this.hashes.Length];
            hashers = new ExpressionHasher[this.hashes.Length];
            for (int i = 0; i < this.hashes.Length; i++)
            {
                comparers[i] = (ExpressionComparer.Factory(this.hashes[i], true, false)); // hash, ascending, no cache.
                hashers[i] = (ExpressionHasher.Factory(this.hashes[i]));
            }
        }

        /// <summary>
        /// Clones comparer and hasher. The cache is always set to true even for the hasher.
        /// </summary>
        protected static void CloneHasherAndComparer(RowEqualityComparerGroupKey comparer, RowHasher hasher, out RowEqualityComparerGroupKey retComparer, out RowHasher retHasher)
        {
            retComparer = comparer.Clone(cacheResults: true);
            retHasher = hasher.Clone();
            retHasher.SetCache(retComparer.comparers);
        }


        /// <summary>
        /// Parses Group by parse tree, the information is stored in the expression info class.
        /// The reason this method is separated from constructor is because it cannot guess whether the 
        /// clause is a normal group by or a single group group by, thus the group by must always be parsed
        /// as the first clause after the Match clause.
        /// </summary>
        public static void ParseGroupBy(Graph graph, VariableMap variableMap, IGroupByExecutionHelper executionHelper, GroupByNode groupByNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || groupByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"Group by results processor, passing null arguments to the constructor.");

            var groupbyVisitor = new GroupByVisitor(graph.labels, variableMap, exprInfo);
            groupbyVisitor.Visit(groupByNode);
            executionHelper.IsSetGroupBy = true;
        }

        /// <summary>
        /// Constructs Group by result processor.
        /// The suffix HS stands for Half Streamed solution, whereas the S stands for Full Streamed solution.
        /// The additional suffices B and L stand for the type of result storages. B is for buckets and L is for Lists.
        /// </summary>
        public static ResultProcessor Factory(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount, int[] usedVars, bool isStreamed)
        {
            if (executionHelper.IsSetSingleGroupGroupBy)
            {
                if (!isStreamed) return new SingleGroupGroupByHalfStreamed(expressionInfo, executionHelper, columnCount, usedVars);
                else return new SingleGroupGroupByStreamed(expressionInfo, executionHelper, columnCount, usedVars);
            }
            else
            {
                if (executionHelper.GrouperAlias == GrouperAlias.GlobalS) return new GlobalGroupByStreamed(expressionInfo, executionHelper, columnCount, usedVars);
                else if (executionHelper.GrouperAlias == GrouperAlias.TwoStepHSB) return new TwoStepHalfStreamedBucket(expressionInfo, executionHelper, columnCount, usedVars);
                else if (executionHelper.GrouperAlias == GrouperAlias.TwoStepHSL) return new TwoStepHalfStreamedListBucket(expressionInfo, executionHelper, columnCount, usedVars);
                else throw new ArgumentException("Group by result processor, trying to create an unknown grouper.");
            }

        }
    }
}
