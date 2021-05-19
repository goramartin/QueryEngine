using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A base class for match object (normal and streamed). The purpose of this class was to provide foundation for the
    /// streamed version of the match since they share common semantics.
    /// </summary>
    internal abstract class MatchObjectBase : QueryObject
    {
        protected IPattern pattern;
        protected IMatchExecutionHelper helper;

        /// <summary>
        /// Throws error when the given pattern is fault.
        /// Fault pattern contains one of:
        /// no variables,
        /// discrepant variable definitions, 
        /// variable used for vertex and edge at the same time,
        /// discrepant type definitions,
        /// repetition of edge variable. 
        /// </summary>
        /// <param name="parsedPatterns"> Pattern to check. </param>
        protected void CheckParsedPatternCorrectness(List<ParsedPattern> parsedPatterns)
        {
            Dictionary<string, ParsedPatternNode> tmpDict = new Dictionary<string, ParsedPatternNode>();
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                var tmpPattern = parsedPatterns[i].pattern;
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
    }
}
