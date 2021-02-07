/*! \file
This file contains factory for match expression.
Matchers and patterns are saved inside a registry.
Each matcher has a set of available patterns.
 */

using System;
using System.Collections.Generic;

namespace QueryEngine
{

    /// <summary>
    /// Class includes register of all the Matchers and their coresponding patterns.
    ///  Enables to create instance of a Matcher/Pattern based on a string token.
    /// </summary>
    internal static class MatchFactory
    {
        /// <summary>
        /// Register of valid matchers.
        /// </summary>
        static Dictionary<string, Type> matcherRegistry;

        /// <summary>
        /// Register of valid patterns for a given matcher.
        /// </summary>
        static Dictionary<string, Dictionary<string, Type>> matcherPatternRegistry;

        /// <summary>
        /// Inicialises registries.
        /// </summary>
        static MatchFactory()
        {
            matcherRegistry = new Dictionary<string, Type>();
            matcherPatternRegistry = new Dictionary<string, Dictionary<string, Type>>();
            InicialiseRegistry();
        }

        /// <summary>
        /// Fills registers with defined data.
        /// </summary>
        private static void InicialiseRegistry()
        {
            RegisterMatcher("DFSSingleThread", typeof(DFSPatternMatcher));
            RegisterMatcher("DFSParallel", typeof(DFSParallelPatternMatcher));
            RegisterMatcher("DFSSingleThreadStreamed", typeof(DFSPatternMatcherStreamed));
            RegisterMatcher("DFSParallelStreamed", typeof(DFSParallelPatternMatcherStreamed));
            RegisterPatternToMatcher("DFSSingleThread", "SIMPLE", typeof(DFSPattern));
            RegisterPatternToMatcher("DFSParallel", "SIMPLE", typeof(DFSPattern));
            RegisterPatternToMatcher("DFSSingleThreadStreamed", "SIMPLE", typeof(DFSPattern));
            RegisterPatternToMatcher("DFSParallelStreamed", "SIMPLE", typeof(DFSPattern));
        }

        /// <summary>
        /// Register a matcher type. Checks if the matcher exists and throws on this occasion.
        /// </summary>
        /// <param name="matcher"> Matcher type. </param>
        /// <param name="type"> Type to register matcher to. </param>
        private static void RegisterMatcher(string matcher, Type type)
        {
            if (matcher == null || type == null)
                throw new ArgumentNullException($"MatchFactory, cannot register null type or null token.");
            else if (matcherRegistry.ContainsKey(matcher))
                throw new ArgumentException($"MatchFactory, matcher Type already registered. Matcher = {matcher}. ");
            else matcherRegistry.Add(matcher, type);
        }


        /// <summary>
        /// Registers a pattern to a given pattern.
        /// </summary>
        /// <param name="matcher"> Matcher type. </param>
        /// <param name="pattern"> Pattern type. </param>
        /// <param name="patternType"> Petter type instante to register. </param>
        private static void RegisterPatternToMatcher(string matcher, string pattern, Type patternType)
        {
            if (matcher == null || pattern == null || patternType == null)
                throw new ArgumentNullException($"MatchFactory, cannot register null type or null token.");


            if (matcherPatternRegistry.TryGetValue(matcher, out Dictionary<string, Type> pDict))
            {
                if (pDict.ContainsKey(pattern))
                    throw new ArgumentException($"MatchFactory, pattern Type already registered to Matcher. Pattern = {pattern}.");
                else pDict.Add(pattern, patternType);
            }
            else
            {
                var tmpDict = new Dictionary<string, Type>();
                tmpDict.Add(pattern, patternType);
                matcherPatternRegistry.Add(matcher, tmpDict);
            }
        }

        /// <summary>
        /// Creates an instante of matcher based on a given matcher type.
        /// </summary>
        /// <param name="matcher"> Matcher type to craete.</param>
        /// <param name="parameters"> Paramters for matcher constructor. </param>
        /// <returns></returns>
        public static IPatternMatcher CreateMatcher(string matcher, params object[] parameters) //IPattern pattern, Graph graph, QueryResults results, int resultIndex)
        {
            if (matcher == null)
                throw new ArgumentNullException($"MatchFactory, cannot access null type or null token.");

            if (matcherRegistry.TryGetValue(matcher, out Type matcherType))
                return (IPatternMatcher)Activator.CreateInstance(matcherType, parameters);
            else throw new ArgumentException($"MatchFactory: Failed to load type from Matcher registry. Matcher = {matcher}.");
        }

        /// <summary>
        /// Creates a pattern type based on given matcher type and pattern type.
        /// </summary>
        /// <param name="matcher"> Matcher type. </param>
        /// <param name="pattern"> Pattern type. </param>
        /// <param name="parameters"> Parameters for pattern constructor. </param>
        /// <returns></returns>
        public static IPattern CreatePattern(string matcher, string pattern, params object[] parameters)
        {
            if (matcher == null || pattern == null)
                throw new ArgumentNullException($"MatchFactory, cannot access null type or null token.");

            if (matcherPatternRegistry.TryGetValue(matcher, out Dictionary<string, Type> pDict))
            {
                if (pDict.TryGetValue(pattern, out Type patternType))
                    return (IPattern)Activator.CreateInstance(patternType, parameters);
                else throw new ArgumentException("MatchFactory: Failed to load type from  Pattern registry.");
            }
            else throw new ArgumentException("MatchFactory: Failed to load type from  Pattern registry.");
        }

    }
}
