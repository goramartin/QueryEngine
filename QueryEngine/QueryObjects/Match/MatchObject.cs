
/*! \file
  
  This file includes definition of match object.
  This class should contain information from the query match expression that is,
  pattern to search and algorithm to perform the search.
  Note that during this class creating happens also definitions 
  of variables to be used by the query, that means it fills the 
  variable map of for the query. (creation of pattern)
  
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Contains Matcher to match pattern in graph.
    /// Also contains pattern to match in main match algorithm also it checks th correctness of the pattern when creating it.
    /// The pattern is created from List of Parsed Patterns passed from Visitor that processes Match expression.
    /// When creating the variable map is filled when constructor of pattern is called and query results are appropriately created
    /// based on number of threads passed and columns which are stored in created pattern.
    /// </summary>
    internal sealed class MatchObject
    {
        private  IPatternMatcher Matcher;
        private  IPattern Pattern;
        private MatchResultsStorage queryResults;

        /// <summary>
        /// Creates Match object.
        /// Creation is done in a few steps.
        /// Firstly the match expression is parsed and traversed with visitors. Visitor returns parsed pattern nodes.
        /// The parsed pattern nodes are used to create a pattern and the created pattern is passed to a matcher constructor.
        /// </summary>
        /// <param name="tokens"> Tokens to be parsed. (Expecting first token to be a Match token.)</param>
        /// <param name="graph"> Graph to conduct a query on. </param>
        /// <param name="variableMap"> Empty map of variables. </param>
        /// <param name="ThreadCount"> Number of threads used for matching.</param>
        /// <param name="VerticesPerRound"> Number of vertices one thread gets per round. Used only if more than one thread is used.</param>
        public MatchObject(List<Token> tokens, VariableMap variableMap, Graph graph, int ThreadCount, int VerticesPerRound = 1 )
        {
            if (tokens == null || variableMap == null || graph == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");

            // Create parse tree of match part of query and
            // create a shallow pattern
            MatchNode matchNode = Parser.ParseMatch(tokens);
            MatchVisitor matchVisitor = new MatchVisitor(graph.NodeTables, graph.EdgeTables);
            matchNode.Accept(matchVisitor);

            //Create real pattern and variableMap
            var result = matchVisitor.GetResult();
            this.CheckParsedPatternCorrectness(result);

            // Create  matcher and pattern based on the name of matcher and pattern
            // Change if necessary //just for testing 
            this.Pattern = MatchFactory.CreatePattern("DFSParallel", "SIMPLE", variableMap, result);
            
            // Now we have got enough information about results. 
            // After creating pattern the variable map is filled and we know extend of the results.
            this.queryResults = new MatchResultsStorage(variableMap.GetCount(), ThreadCount);

            this.Matcher = MatchFactory.CreateMatcher("DFSParallel", Pattern, graph, this.queryResults, ThreadCount, VerticesPerRound);
        }

        /// <summary>
        /// Throws error when the given pattern is fault.
        /// Fault pattern contains one of: No variables, 
        /// discrepant variable definitions, 
        /// variable used for vertex and edge at the same time,
        /// discrepant type definitions,
        /// repetition of edge variable. 
        /// </summary>
        /// <param name="parsedPatterns"> Patterns to check. </param>
        private void CheckParsedPatternCorrectness(List<ParsedPattern> parsedPatterns)
        {
            Dictionary<string, ParsedPatternNode> tmpDict = new Dictionary<string, ParsedPatternNode>();
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                var tmpPattern = parsedPatterns[i].Pattern;
                for (int j = 0; j < tmpPattern.Count; j++)
                {
                    string name = tmpPattern[j].Name;
                    // Anonymous variables are skipped.
                    if (name == null) continue;
                    // Try to obtain variable with the same name, if it is missing insert it to dictionary.
                    if (!tmpDict.TryGetValue(name, out ParsedPatternNode node)) tmpDict.Add(name, tmpPattern[j]);
                    else
                    {   // Compare the two variables with the same name.
                        if (!node.Equals(tmpPattern[j]))
                            throw new ArgumentException($"{this.GetType()}, variables from Match expr are not matching."); 
                        // Check if the same variables are edges -> edges cannot be repeated.
                        else if ((node is EdgeParsedPatternNode) && (tmpPattern[j] is EdgeParsedPatternNode))
                            throw new ArgumentException($"{this.GetType()}, you cannot repeat edge variables in match expression.");
                        else continue;
                    }
                }
            }
            // Check if at least one variable was found.
            if (tmpDict.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, no given variable in the query.");
        }

        /// <summary>
        /// Starts searching of the graph.
        /// </summary>
        /// <returns> Results of search algorithm </returns>
        public IResults Search()
        {
            this.Matcher.Search();
            var tmp = new Results(this.queryResults.GetResults());
            return tmp;
        }
    }
}


