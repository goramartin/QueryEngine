/*! \file
  
This file includes definition of pattern used by a dfs match algorithm.
  
Pattern itself is created from ParsedPattern nodes that visitor of match expression tree collects.
The chains are sorted and connected so that they form a connected pattern (if the chains are connected.
Connected, that means that they share a variable.)
This allows the search algorithm iterate over chains in forward manner and choose the right vertices to start search for the 
particular pattern without repeating any unneccessary iterations. Also if the same variable occurs,
the chain might be split into two chains  (a) - (b) - (c) where the repeating variable is b
New chains will be (b) - (a), (b) - (c). This helps search algorithm connect chains together, because newly created chains,
it can be simply connected with variable b and proceed forward to -(a), -(c) without the need to go backwards to (a)-.

The matching algorith tries to apply in every step elements it finds and the matcher can ask the pattern if it reached end of chain
or end of entire pattern. During matching, variables are stored in a scope. Then they can be access properly with the indeces
stored in the variable map. Hence, only elements from scope are copies into result tables.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class that implements basic DFS pattern.
    /// Creates it self from parsed pattern.
    /// Pattern is represented by the lists of base match nodes.
    /// Also it remembers the state of the matched variables and state
    /// which nodes and chains should be matched.
    /// The matched variables are stored in the scope.
    /// </summary>
    internal sealed class DFSPattern : IDFSPattern
    {
        private List<List<DFSBaseMatch>> patterns;
        public int CurrentPatternIndex { get; private set; }
        public int CurrentMatchNodeIndex { get; private set; }
        public int OverAllIndex { get; private set; }
        public int PatternCount { get => this.patterns.Count; }
        public int CurrentPatternCount { get => this.patterns[this.CurrentPatternIndex].Count; }
        public int AllNodeCount 
        { 
            get
            {
                int Count = 0;
                for (int i = 0; i < this.patterns.Count; i++)
                {
                    Count += this.patterns[i].Count;
                }
                return Count;
            }
        }

        /// <summary>
        /// Map of variables that maps strings (names of variables) in map for the whole query.
        /// We need to ensure that the indexes when retrieved are the same. 
        /// Integer here is the positionOfRepeated variable (index in Map of variables.)
        /// </summary>
        private Element[] scope;

        /// <summary>
        /// Creates a pattern from a given matches. Used only inside clone method.
        /// </summary>
        /// <param name="dFSBaseMatches"> Pattern to match during search. </param>
        /// <param name="variableCount"> Number of variables for the scope. </param>
        private DFSPattern(List<List<DFSBaseMatch>> dFSBaseMatches, int variableCount)
        {
            if (dFSBaseMatches == null || dFSBaseMatches.Count == 0) 
                throw new ArgumentException($"{this.GetType()} passed null or empty matches.");

            this.patterns = dFSBaseMatches;
            this.scope = new Element[variableCount];
            this.CurrentMatchNodeIndex = 0;
            this.CurrentPatternIndex = 0;
            this.OverAllIndex = 0;
        }

        /// <summary>
        /// Creates a pattern from parsed patterns.
        /// Creation is done as follow: Ordering of patters (based on connection between chains)
        /// then spliting is done if neccessary in order to ensure that match algorithm can match only forward, then 
        /// proper match nodes are created with a match node factory.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="parsedPatterns"></param>
        public DFSPattern(VariableMap map, List<ParsedPattern> parsedPatterns)
        {
            if (parsedPatterns == null || parsedPatterns.Count == 0) 
                throw new ArgumentException($"{this.GetType()} passed null or empty parsed pattern.");

            this.patterns = new List<List<DFSBaseMatch>>();
            this.CreatePattern(parsedPatterns, map);

            this.scope = new Element[map.GetCount()];
            this.scope.Populate(null);

            this.CurrentMatchNodeIndex = 0;
            this.CurrentPatternIndex = 0;
            this.OverAllIndex = 0;
        }

        #region PatternCreation
        /// <summary>
        /// Creates pattern from Parsed Pattern passed from match visitor, also actualises a map for variables.
        /// Given pattern is ordered so that each connected pattern go after one another and form a connected components. 
        /// Subsequently, the resulting pattern formed by base match node is created.
        /// Patterns to be splited are splited into two based on split variable. So that match algorithm can match only forward. 
        /// For example: (a) - (b) - (c) splited by var. b == (b) - (a) , (b) - (c)
        /// Note that (a) - (b) was reversed in order ( oriented edges are reversed as well )
        /// </summary>
        /// <param name="parsedPatterns"> Pattern created by Match Visitor </param>
        /// <param name="variableMap"> Query map of variables (empty) </param>
        private void CreatePattern(List<ParsedPattern> parsedPatterns, VariableMap variableMap)
        {
            var orderedPatterns = OrderParsedPatterns(parsedPatterns);

            // For every Parsed Pattern
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                // Try to split pattern into two parts.
                // If split, then only the first part is returned and the second one is stored at the parsedPatterns[i]
                var firstPart = orderedPatterns[i].TrySplitParsedPattern();

                // If the parsed pattern was splited
                // Add both parts into the real pattern
                if (firstPart != null)
                {
                    this.patterns.Add(CreateChain(firstPart.Pattern, variableMap));
                }
                // Always add the second part even if the has not been splitted.
                this.patterns.Add(CreateChain(orderedPatterns[i].Pattern, variableMap));
            }

            // Check that each subpattern has at least one element.
            for (int i = 0; i < this.patterns.Count; i++)
                if (this.patterns[i].Count == 0) 
                    throw new ArgumentException($"{this.GetType()} one of the patterns is empty.");

        }

        /// <summary>
        /// Purpose of the ordering is to ensure that the chains in the final list form
        /// a connected components and to find the variable that the chains are connected by.
        /// By doing so, the matching algorithm can simply start dfs by picking the variable that the chains were
        /// connected by. If there are more separate connected components. They are put after one another.
        /// 
        /// Unused patterns are pattern that were not placed into final ordered result.
        /// Connected pattern indeces queue is a queue for indeces of pattern that can be connected by currently 
        /// processed pattern.
        /// Ordering is done as follows:
        /// For every pattern -
        /// 1. if it has not been used, pick him and add him to final results, mark it as used
        ///    and enqueue its index adn proceed to step 2.
        /// 2. We enequeud the index. Iterate over all unused parsed patterns and check for common variables.
        ///    If a common variable was found, put the chain into results and set his split by variable and enqueue him.
        ///    Repeat until the queue is empty.
        /// Note that patterns that are not connected or are firstly picked have not got set their split by variable.
        /// </summary>
        /// <param name="parsedPatterns"> Parser Patterns from MatchVisitor. </param>
        /// <returns> Ordered parsed patterns. </returns>
        private List<ParsedPattern> OrderParsedPatterns(List<ParsedPattern> parsedPatterns)
        {
            List<ParsedPattern> result = new List<ParsedPattern>();
            bool[] usedPatterns = new bool[parsedPatterns.Count];
            usedPatterns.Populate(false);
            Queue<int> connectedPatternsIndeces = new Queue<int>();

            // For every pattern
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                // If it is used already, skip to the next one
                if (usedPatterns[i] == true) continue;
                else
                {
                    // Else add it to the results, mark it as used and enqueu it
                    result.Add(parsedPatterns[i]); 
                    usedPatterns[i] = true;
                    connectedPatternsIndeces.Enqueue(i);
                    while (connectedPatternsIndeces.Count != 0)
                    {
                        // Take the last inserted pattern and try to iterate over all other unused patterns and check
                        // for common variables
                        ParsedPattern currentPattern = parsedPatterns[connectedPatternsIndeces.Dequeue()];
                        for (int j = 0; j < parsedPatterns.Count; j++)
                        {
                            // If the pattern is used, it was already connected by other pattern
                            // else we can connected it to the results.
                            if (usedPatterns[j] == true) continue;
                            else if (currentPattern.TryFindEqualVariable(parsedPatterns[j], out string name)){
                                result.Add(parsedPatterns[j]);
                                usedPatterns[j] = true;
                                parsedPatterns[j].splitBy = name;
                                connectedPatternsIndeces.Enqueue(j);
                            } else { /* continue */}
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a pattern chain formed by base matches that create a final pattern used in a matcher.
        /// Iterates over a list of given pattern nodes and creates
        /// appropriate base match classes with the attributes based on properties
        /// of each pattern node. During this iteration, the variable map for entire
        /// query is actualised. (The names of pattern nodes represent variables.)
        /// And the order of added variable form an order that will be used to access each variable throughout
        /// the entire query computation.
        /// </summary>
        /// <param name="patternNodes"> Parsed pattern </param>
        /// <param name="map"> A map to store info about variables. </param>
        /// <returns> Chain of base matches for the search algorithm. </returns>
        private List<DFSBaseMatch> CreateChain(List<ParsedPatternNode> patternNodes, VariableMap map)
        {
            List<DFSBaseMatch> tmpChain = new List<DFSBaseMatch>();

            // For each parsed pattern node
            for (int i = 0; i < patternNodes.Count; i++)
            {
                var tmpNode = patternNodes[i];
                int index = -1;
                bool isFirst = true;

                // If it has not got a name, do not add it to the variable map.
                if (tmpNode.Name != null)
                {
                    // Try if the variable is inside a dictionary
                    if ((index = map.GetVariablePosition(tmpNode.Name)) == -1)
                    {
                        // If it is not, Add it there with the proper type and index.
                        // Note: Table can be null
                        index = map.GetCount();
                        map.AddVariable(tmpNode.Name, index, tmpNode.Table);
                    }
                    else isFirst = false;
                }

                // Create match node and add it to the chain.
                tmpChain.Add(DFSBaseMatch.DFSBaseMatchFactory(tmpNode, index, isFirst));
            }
            return tmpChain;
        }

        #endregion PatternCreation

        #region PatternInterface

        /// <summary>
        /// Calls apply on match object. The element is checked whether it sufficces condition if yes,
        /// the scope is actualised if necessary.
        /// </summary>
        /// <param name="element"> Element to be tested. </param>
        /// <returns> True if the element can be applied, false if it cannot be applied. </returns>
        public bool Apply(Element element)
        {
            return this.patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex].Apply(element, this.scope);
        }

        /// <summary>
        /// Prepares subsequent pattern (Moving in dfs forward). The only need is to actualise the indeces. It proceed to the next pattern,
        /// thus current pattern must be increased and position of base match node set to start. 
        /// </summary>
        public void PrepareNextSubPattern()
        {
            this.CurrentPatternIndex++;
            this.CurrentMatchNodeIndex = 0;
            this.OverAllIndex++;
        }

        /// <summary>
        /// Algorithm should ensure that the method is not called after the first pattern.
        /// Prepares previous pattern (Moving backwards in dfs). Only indeces must be reduced, variables were unset
        /// during return from each base match node.
        /// </summary>
        public void PreparePreviousSubPattern()
        {
            this.CurrentPatternIndex--;
            this.CurrentMatchNodeIndex = this.CurrentPatternCount - 1;
            this.OverAllIndex--;
        }

        /// <summary>
        /// The algorithm should ensure that the method is not called after the last match node.
        /// Moves indeces to the next match node.
        /// Proceeds to next element to be matched.
        /// </summary>
        public void PrepareNextNode()
        {
            this.CurrentMatchNodeIndex++;
            this.OverAllIndex++;
        }

        /// <summary>
        /// Proceeds to previous element that was matched.
        /// If the current node is not anonymous, we need to reset variable inside the scope.
        /// The unset method is called even if the variable is not set.
        /// </summary>
        public void PreparePreviousNode()
        {
            this.UnsetCurrentVariable();
            if (this.CurrentMatchNodeIndex != 0)
            {
                this.OverAllIndex--;
                this.CurrentMatchNodeIndex--;
            }
        }

        /// <summary>
        /// Unsets the variable representing current matched node from the scope.
        /// </summary>
        public void UnsetCurrentVariable()
        {
            this.patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex].UnsetVariable(this.scope);
        }

        /// <summary>
        /// Gets starting element of the current chain (if it was connected to the ones before).
        /// </summary>
        /// <returns> Null if anonymous/firstAppearance else it is picked from the scope. </returns>
        public Element GetCurrentChainConnection()
        {
            return this.patterns[this.CurrentPatternIndex][0].GetVariable(this.scope);
        }

        /// <summary>
        /// Gets starting element of the next chain (if it was connected to the ones before).
        /// This method is called only when there is another pattern to be processed.
        /// If the next chain contains a variable that was already used it returns it from the scope.
        /// </summary>
        /// <returns>Null if anonymous/first appearance else element from scope. </returns>
        public Element GetNextChainConnection()
        {
            return this.patterns[this.CurrentPatternIndex + 1][0].GetVariable(this.scope);
        }

        public bool IsLastNodeInCurrentPattern()
        {
            return (this.patterns[this.CurrentPatternIndex].Count - 1) == this.CurrentMatchNodeIndex ? true : false;
        }

        public bool IsLastPattern()
        {
            return this.CurrentPatternIndex == (this.patterns.Count - 1) ? true : false;
        }

       /// <summary>
       /// Returns type of the graph element represented by the current match node.
       /// </summary>
        public Type GetMatchType()
        {
            return (this.patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex]).GetMatchType();
        }

        /// <summary>
        /// Shallow copy of a pattern.
        /// </summary>
        public IDFSPattern Clone()
        {
            var tmpPattern = new DFSPattern(this.patterns, this.scope.Length);
            return tmpPattern;
        }

        /// <summary>
        /// Returns all variables that have been matched so far.
        /// </summary>
        public Element[] GetMatchedVariables()
        {
            return this.scope;
        }

        #endregion PatternInterface
    }


}
