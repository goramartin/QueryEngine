
/**
 * This file includes defintion of match object.
 * This class should contain information from the query match expression that is,
 * pattern to search and algorithm to perform the search.
 * Note that during this class creating happens also definitions 
 * of variables to be used by the query, that means it fills the 
 * variable map of for the query. (creation of pattern)
 * 
 * 
 * This file also contains definitoin of static factory for matcher and pattern.
 * */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Match represents pattern to match in main match algorithm also it checks th correctness of the pattern when creating it.
    /// The pattern is created from List of Parsed Patterns passed from Visitor that processes Match expression.
    /// </summary>
    class MatchObject
    {
        private  IPatternMatcher Matcher;
        private  IPattern Pattern;
        public IResultStorage queryResults;

        /// <summary>
        /// Creates Match expression
        /// </summary>
        /// <param name="tokens"> Tokens to be parsed. (Expecting first token to be a Match token.)</param>
        /// <param name="graph"> Graph to be conduct a query on. </param>
        /// <param name="variableMap"> Empty map of variables. </param>
        public MatchObject(List<Token> tokens, VariableMap variableMap, Graph graph)
        {
            if (tokens == null || variableMap == null || graph == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");


            // Create parse tree of match part of query and
            // create a shallow pattern
            MatchNode matchNode = Parser.ParseMatchExpr(tokens);
            MatchVisitor matchVisitor = new MatchVisitor(graph.NodeTables, graph.EdgeTables);
            matchNode.Accept(matchVisitor);

            //Create real pattern and variableMap
            var result = matchVisitor.GetResult();
            this.CheckParsedPatternCorrectness(result);

            // Create  matcher and pattern based on the name of matcher and pattern
            // Change if necessary just for testing 
            this.Pattern = MatchFactory.CreatePattern("DFSParallel", "SIMPLE", variableMap, result);
            
            // Now we have got enough information about results. 
            // After creating pattern the variable map is filled and we know extend of the results.
            this.queryResults = new QueryResults(variableMap.GetCount(), QueryEngine.ThreadsPerQuery);

            this.Matcher = MatchFactory.CreateMatcher("DFSParallel", Pattern, graph, this.queryResults);
        }

        /// <summary>
        /// Throws error when the given pattern is fault.
        /// Fault pattern contains one of: No variables, Discrepant variable definitions
        /// discrepant type definitions
        /// Correctness is checked only against the first appearance of the variable.
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
                    string name = tmpPattern[j].GetName();
                    // Anonymous variables are skipped.
                    if (name == null) continue;
                    // Try to obtain variable with the same name, if it is missing insert it to dictionary.
                    if (!tmpDict.TryGetValue(name, out ParsedPatternNode node)) tmpDict.Add(name, tmpPattern[j]);
                    else
                    {   // Compare the two variables with the same name.
                        if (!node.Equals(tmpPattern[j])) 
                            throw new ArgumentException($"{this.GetType()}, variables from Match expr are not matching.");
                        else continue;
                    }
                }
            }
            // Check if at least one variable was found.
            if (tmpDict.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, no given variable in the query.");
        }

        //just for testing
        public IPattern GetPattern() => this.Pattern;
        public IPatternMatcher GetMatcher() => this.Matcher;
    }


  
   




    /// <summary>
    /// Class includes register of all the Matchers and their coresponding patterns.
    ///  Enables to create instance of a Matcher/Pattern based on a string token.
    /// </summary>
    static class MatchFactory
    {
        static Dictionary<string, Type> MatcherRegistry;
        static Dictionary<string, Dictionary<string, Type>> MatcherPatternRegistry;

        static MatchFactory()
        {
            MatcherRegistry = new Dictionary<string, Type>();
            MatcherPatternRegistry = new Dictionary<string, Dictionary<string, Type>>();
            InicialiseRegistry();
        }

        private static void InicialiseRegistry()
        {
            RegisterMatcher("DFSSingleThread", typeof(DFSPatternMatcher));
            RegisterMatcher("DFSParallel", typeof(DFSParallelPatternMatcher));
            RegisterPatternToMatcher("DFSSingleThread", "SIMPLE", typeof(DFSPattern));
            RegisterPatternToMatcher("DFSParallel", "SIMPLE", typeof(DFSPattern));
        }

        private static void RegisterMatcher(string matcher, Type type)
        {
            if (matcher == null || type == null)
                throw new ArgumentNullException($"MatchFactory, cannot register null type or null token.");


            if (MatcherRegistry.ContainsKey(matcher))
                throw new ArgumentException($"MatchFactory, matcher Type already registered. Matcher = {matcher}. ");

            MatcherRegistry.Add(matcher, type);
        }


        private static void RegisterPatternToMatcher(string matcher, string pattern, Type patternType)
        {
            if (matcher == null|| pattern == null || patternType == null)
                throw new ArgumentNullException($"MatchFactory, cannot register null type or null token.");


            if (MatcherPatternRegistry.TryGetValue(matcher, out Dictionary<string,Type> pDict))
            {
                if (pDict.TryGetValue(pattern, out Type value))
                    throw new ArgumentException($"MatchFactory, pattern Type already registered to Matcher. Pattern = {pattern}.");
                else pDict.Add(pattern, patternType); 

            } else {
                var tmpDict = new Dictionary<string, Type>();
                tmpDict.Add(pattern, patternType);
                MatcherPatternRegistry.Add(matcher, tmpDict);
            }
        }

        public static IPatternMatcher CreateMatcher(string matcher, params object[] parameters) //IPattern pattern, Graph graph, QueryResults results, int resultIndex)
        {
            if (matcher == null)
                throw new ArgumentNullException($"MatchFactory, cannot access null type or null token.");

            Type matcherType = null;
            if (MatcherRegistry.TryGetValue(matcher, out matcherType))
            {
                return (IPatternMatcher)Activator.CreateInstance(matcherType, parameters);
            }
            else throw new ArgumentException($"MatchFactory: Failed to load type from Matcher registry. Matcher = {matcher}.");
        }

        public static IPattern CreatePattern(string matcher, string pattern, params object[] parameters)
        {
            if (matcher == null || pattern == null)
                throw new ArgumentNullException($"MatchFactory, cannot access null type or null token.");

            if (MatcherPatternRegistry.TryGetValue(matcher, out Dictionary<string, Type> pDict))
            {
                if (pDict.TryGetValue(pattern, out Type patternType)) 
                 return (IPattern)Activator.CreateInstance(patternType, parameters);
                else throw new ArgumentException("MatchFactory: Failed to load type from  Pattern registry.");
            }
            else throw new ArgumentException("MatchFactory: Failed to load type from  Pattern registry.");
        }

    }
}


