﻿/*! \file
This file includes definition of match object.
This class should contain information from the query match expression that is,
pattern to search and algorithm to perform the search.
Note that during this class creation happens also definitions 
of variables to be used by the entire query, that means it fills the 
variable map of for the query.
*/

using System;

namespace QueryEngine
{
    /// <summary>
    /// Contains a Matcher to match the pattern in graph.
    /// Also contains a pattern to match in main match algorithm.
    /// It checks th correctness of the pattern when creating it.
    /// The pattern is created from List of ParsedPatterns classes passed from Visitor that processes Match expression.
    /// The variable map is filled when constructor of pattern is called.
    /// </summary>
    internal sealed class MatchObject : MatchObjectBase
    {
        private MatchFixedResults queryResults;
        private  IPatternMatcher matcher;

        /// <summary>
        /// Creates a Match object.
        /// </summary>
        /// <param name="graph"> A graph to conduct a query on. </param>
        /// <param name="variableMap"> An empty map of variables. </param>
        /// <param name="executionHelper"> A match execution helper. </param>
        /// <param name="matchNode"> A parse tree of match expression. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public MatchObject(Graph graph, VariableMap variableMap, IMatchExecutionHelper executionHelper, MatchNode matchNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || matchNode == null || variableMap == null || graph == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            this.helper = executionHelper;
            MatchVisitor matchVisitor = new MatchVisitor(graph.nodeTables, graph.edgeTables);
            matchNode.Accept(matchVisitor);

            // Create real pattern and variableMap.
            var result = matchVisitor.GetResult();
            this.CheckParsedPatternCorrectness(result);

            // Create  matcher and pattern based on the name of matcher and pattern.
            // Change if necessary .
            this.pattern = MatchFactory.CreatePattern(helper.ParallelPatternMatcherName, helper.PatternName, variableMap, result);
            
            // Now we have got enough information about results. 
            // After creating pattern the variable map is filled and we know extend of the results.
            this.queryResults = new MatchFixedResults(this.helper.FixedArraySize, variableMap.GetCount(), executionHelper.ThreadCount);

            this.matcher = MatchFactory.CreateMatcher(helper.ParallelPatternMatcherName, pattern, graph, this.queryResults, executionHelper);
        }

        public override void Compute(out ITableResults resTable, out GroupByResults groupByResults)
        {
            if (next != null)
                throw new Exception($"{this.GetType()}, there was an execution block after match block.");
            else
            {
                resTable = this.Search();
                groupByResults = null;
            }
        }

        /// <summary>
        /// Starts searching of the graph and returns results of the search.
        /// </summary>
        /// <returns> The results of search algorithm </returns>
        private ITableResults Search()
        {
            this.matcher.Search();
            return new TableResults(this.queryResults.FinalMerged, this.queryResults.NumberOfMatchedElements, this.queryResults.FixedArraySize, this.helper.IsStoringResult);
        }

        public override void PassStoringVariables(int[] vars)
        {
            this.queryResults.PassStoringVariables(vars);
        }
    }
}


