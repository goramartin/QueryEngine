using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A base class for every group result processor.
    /// </summary>
    internal abstract class GroupResultProcessor : ResultProcessor
    {
        protected Aggregate[] aggregates;
        protected ExpressionHolder[] hashes;
        protected IGroupByExecutionHelper executionHelper;
        /// <summary>
        /// Represents a number of variables defined in the match clause of the query.
        /// </summary>
        protected int ColumnCount { get; }

        protected GroupResultProcessor(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount)
        {
            this.aggregates = expressionInfo.Aggregates.ToArray();
            this.hashes = expressionInfo.GroupByhashExprs.ToArray();
            this.executionHelper = executionHelper;
            this.ColumnCount = columnCount;
        }

        protected virtual void CreateHashersAndComparers(out ExpressionEqualityComparer[] comparers, out ExpressionHasher[] hashers)
        {
            comparers = new ExpressionEqualityComparer[this.hashes.Length];
            hashers = new ExpressionHasher[this.hashes.Length];
            for (int i = 0; i < this.hashes.Length; i++)
            {
                comparers[i] = (ExpressionEqualityComparer.Factory(this.hashes[i], this.hashes[i].ExpressionType));
                hashers[i] = (ExpressionHasher.Factory(this.hashes[i], this.hashes[i].ExpressionType));
            }
        }

        /// <summary>
        /// Parses Group by node tree, the information is stored in the expression info class.
        /// </summary>
        public static void ParseGroupBy(Graph graph, VariableMap variableMap, IGroupByExecutionHelper executionHelper, GroupByNode groupByNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || groupByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"Group by results processor, passing null arguments to the constructor.");

            var groupbyVisitor = new GroupByVisitor(graph.labels, variableMap, exprInfo);
            groupbyVisitor.Visit(groupByNode);
            executionHelper.IsSetGroupBy = true;
        }

        public static ResultProcessor Factory(QueryExpressionInfo expressionInfo, IGroupByExecutionHelper executionHelper, int columnCount)
        {
            if (executionHelper.GrouperAlias == "singleHS") return new SingleGroupResultProcessorHalfStreamed(expressionInfo, executionHelper, columnCount);
            else if (executionHelper.GrouperAlias == "singleS") return new SingleGroupResultProcessorStreamed(expressionInfo, executionHelper, columnCount);
            else if (executionHelper.GrouperAlias == "globalS") return new GlobalGroupStreamed(expressionInfo, executionHelper, columnCount);
            else if (executionHelper.GrouperAlias == "twowayHSB") return new LocalGroupGlobalMergeHalfStreamedBucket(expressionInfo, executionHelper, columnCount);
            else if (executionHelper.GrouperAlias == "twowayHSL") return new LocalGroupGlobalMergeHalfStreamedListBucket(expressionInfo, executionHelper, columnCount);
            else throw new ArgumentException("Group by result processor, trying to create an unknown grouper.");
        }
    }
}
