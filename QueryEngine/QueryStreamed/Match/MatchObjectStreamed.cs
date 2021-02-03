using System;

namespace QueryEngine
{   

    /// <summary>
    /// It works the same as the normal MatchObject, except it allow to pass result processors to the matchers.
    /// </summary>
    internal class MatchObjectStreamed : MatchObjectBase
    {
        private IPatternMatcherStreamed matcher;
        private ResultProcessor resultProcessor = null;

        /// <summary>
        /// Creates Streamed Match object.
        /// </summary>
        /// <param name="graph"> Graph to conduct a query on. </param>
        /// <param name="variableMap"> Empty map of variables. </param>
        /// <param name="executionHelper"> Match execution helper. </param>
        /// <param name="matchNode"> Parse tree of match expression. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public MatchObjectStreamed(Graph graph, VariableMap variableMap, IMatchExecutionHelper executionHelper, MatchNode matchNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || matchNode == null || variableMap == null || graph == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            this.helper = executionHelper;
            MatchVisitor matchVisitor = new MatchVisitor(graph.nodeTables, graph.edgeTables);
            matchNode.Accept(matchVisitor);

            //Create real pattern and variableMap
            var result = matchVisitor.GetResult();
            this.CheckParsedPatternCorrectness(result);

            // Create  matcher and pattern based on the name of matcher and pattern
            // Change if necessary 
            this.pattern = MatchFactory.CreatePattern(helper.ParallelPatternMatcherName, helper.PatternName, variableMap, result);
            this.matcher = (IPatternMatcherStreamed)MatchFactory.CreateMatcher(helper.ParallelPatternMatcherName, pattern, graph, executionHelper);
        }

        public override void Compute(out ITableResults results, out GroupByResults groupByResults)
        {
            if (next != null)
                throw new Exception($"{this.GetType()}, there was an execution block after match block.");
            this.matcher.Search();
            this.resultProcessor.RetrieveResults(out results, out groupByResults);
        }

        public void PassResultProcessor(ResultProcessor resultProcessor)
        {
            if (resultProcessor == null)
                throw new ArgumentNullException($"{this.GetType()}, result processor cannot be null.");
            else
            {
                this.resultProcessor = resultProcessor;
                this.matcher.PassResultProcessor(resultProcessor);
            }
        }
    }
}
