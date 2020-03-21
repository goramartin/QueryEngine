


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
        public QueryResults queryResults;

        /// <summary>
        /// Creates Match expression
        /// </summary>
        /// <param name="tokens"> Tokens to be parsed. (Expecting first token to be a Match token.)</param>
        /// <param name="graph"> Graph to be conduct a query on. </param>
        /// <param name="variableMap"> Empty map of variables. </param>
        /// <param name="results"> Place to store search results. </param>
        public MatchObject(List<Token> tokens, VariableMap variableMap, Graph graph, QueryResults queryResults)
        {
            this.queryResults = queryResults;

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
            this.Matcher = MatchFactory.CreateMatcher("DFSParallel", Pattern, graph, queryResults);
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
                        if (!node.Equals(tmpPattern[j])) throw new ArgumentException($"{this.GetType()} Variables from Match expr are not matching.");
                        else continue;
                    }
                }
            }
            // Check if at least one variable was found.
            if (tmpDict.Count == 0) throw new ArgumentException($"{this.GetType()} No given variable in the query.");
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
            if (MatcherRegistry.ContainsKey(matcher))
                throw new ArgumentException("MatchFactory: Matcher Type already registered.");

            MatcherRegistry.Add(matcher, type);
        }


        private static void RegisterPatternToMatcher(string matcher, string pattern, Type patternType)
        {
            if (MatcherPatternRegistry.TryGetValue(matcher, out Dictionary<string,Type> pDict))
            {
                if (pDict.TryGetValue(pattern, out Type value))
                    throw new ArgumentException("MatchFactory: Pattern Type already registered to Matcher.");
                else pDict.Add(pattern, patternType); 

            } else {
                var tmpDict = new Dictionary<string, Type>();
                tmpDict.Add(pattern, patternType);
                MatcherPatternRegistry.Add(matcher, tmpDict);
            }
        }

        public static IPatternMatcher CreateMatcher(string matcher, params object[] parameters) //IPattern pattern, Graph graph, QueryResults results, int resultIndex)
        {
            if (!MatcherRegistry.ContainsKey(matcher))
                throw new ArgumentException("MatchFactory: Matcher Token not found.");

            Type matcherType = null;
            if (MatcherRegistry.TryGetValue(matcher, out matcherType))
            {
                return (IPatternMatcher)Activator.CreateInstance(matcherType, parameters);
            }
            else throw new ArgumentException("MatchFactory: Failed to load type from Matcher registry.");
        }

        public static IPattern CreatePattern(string matcher, string pattern, params object[] parameters)
        {
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


