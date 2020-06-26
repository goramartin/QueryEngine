/*! \file
  
  This file includes definition of pattern used by a dfs match algorithm.
  
  Pattern itself is created from ParsedPattern nodes that visitor of match expression tree collects.
  The chains are sorted and connected so that they form a connected pattern if the chains are connected.
  (Connected, that means that they share a variable.)
  This allows the search algorithm iterate over chains and choose the right vertices to start search for the 
  particular pattern without repeating any unneccessary iterations. Also if the same variable occurs,
  the chain might be split into two chains  (a) - (b) - (c) where the repeating variable is b
  New chains will be (b) - (a), (b) - (c). This helps search algorithm connect chains together.
 
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
    /// Pattern is represented by the lists of matching nodes.
    /// Also it remembers the state of the matched variables and state
    /// which nodes and chains should be matched.
    /// The matched variables are stored in the scope.
    /// </summary>
    internal sealed class DFSPattern : IDFSPattern
    {
        private List<List<DFSBaseMatch>> Patterns;
        public int CurrentPatternIndex { get; private set; }
        public int CurrentMatchNodeIndex { get; private set; }
        public int OverAllIndex { get; private set; }
        public int PatternCount { get => this.Patterns.Count; }
        public int CurrentPatternCount { get => this.Patterns[this.CurrentPatternIndex].Count; }
        public int AllNodeCount 
        { 
            get
            {
                int Count = 0;
                for (int i = 0; i < this.Patterns.Count; i++)
                {
                    Count += this.Patterns[i].Count;
                }
                return Count;
            }
        }

        /// <summary>
        /// Map of variables that maps strings (names of variables) in map for the whole query.
        /// We need to ensure that the indexes when retrieved are the same. 
        /// Integer here is the positionOfRepeated variable (index in Map of variables.)
        /// </summary>
        private Element[] Scope;

        /// <summary>
        /// Creates a pattern from a given matches. Used only inside clone method.
        /// </summary>
        /// <param name="dFSBaseMatches"> Pattern to match during search. </param>
        /// <param name="variableCount"> Number of variables for the scope. </param>
        private DFSPattern(List<List<DFSBaseMatch>> dFSBaseMatches, int variableCount)
        {
            if (dFSBaseMatches == null || dFSBaseMatches.Count == 0) 
                throw new ArgumentException($"{this.GetType()} passed null or empty matches.");

            this.Patterns = dFSBaseMatches;
            this.Scope = new Element[variableCount];
            this.CurrentMatchNodeIndex = 0;
            this.CurrentPatternIndex = 0;
            this.OverAllIndex = 0;
        }

        /// <summary>
        /// Creates a pattern from parsed patterns.
        /// Creation is done as follow: Ordering of patters (based on connection between chains)
        /// then spliting is done if neccessary in order to make search alogirth linear then 
        /// proper match nodes are created with match node factory.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="parsedPatterns"></param>
        public DFSPattern(VariableMap map, List<ParsedPattern> parsedPatterns)
        {
            if (parsedPatterns == null || parsedPatterns.Count == 0) 
                throw new ArgumentException($"{this.GetType()} passed null or empty parsed pattern.");

            this.Patterns = new List<List<DFSBaseMatch>>();
            this.CreatePattern(parsedPatterns, map);

            this.Scope = new Element[map.GetCount()];
            this.Scope.Populate(null);

            this.CurrentMatchNodeIndex = 0;
            this.CurrentPatternIndex = 0;
            this.OverAllIndex = 0;
        }

        #region PatternCreation
        /// <summary>
        /// Creates pattern from Parsed Pattern made by match visitor, also creates a map for variables
        /// during pattern matching.
        /// Given pattern is check for correctness and ordered so each connected patterns go after each 
        /// Then the resulting pattern is created. Patterns to be splited are splited into two based on split variable.
        /// For example: (a) - (b) - (c) splited by var. b == (b) - (a) , (b) - (c)
        /// </summary>
        /// <param name="parsedPatterns"> Pattern created by Match Visitor </param>
        /// <param name="variableMap"> Query map of variables (empty) </param>
        private void CreatePattern(List<ParsedPattern> parsedPatterns, VariableMap variableMap)
        {
            var orderedPatterns = OrderParsedPatterns(parsedPatterns);

            Console.ReadLine();
            // For every Parsed Pattern
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                // Try to split pattern into two parts. Only the first part is return and the second one is stored at the parsedPatterns[i]
                var firstPart = orderedPatterns[i].TrySplitParsedPattern();

                // If the parsed pattern was splited
                // Add both parts into the real Pattern
                if (firstPart != null)
                {
                    this.Patterns.Add(CreateChain(firstPart.Pattern, variableMap));
                }
                // Always add the second part even if the has not been splitted.
                this.Patterns.Add(CreateChain(orderedPatterns[i].Pattern, variableMap));
            }

            // Check that each subpattern has at least one element.
            for (int i = 0; i < this.Patterns.Count; i++)
            {
                if (this.Patterns[i].Count == 0) throw new ArgumentException($"{this.GetType()} one of the patterns is empty.");
            }

        }

        /// <summary>
        /// Orders patterns so that each consecutive pattern can be connected with patterns before.
        /// Map used patterns in an array of bools (true meaning that the pattern has been ordered). If we found two patterns that have the same variable,
        /// then check if one of these patterns was ordered and do following:
        /// 1, If the first was ordered and the second did not, then, we add the second to ordered sequence, mark that
        ///    it was ordered and set its split variable to the common variable.
        /// 2, If the first was not ordered and the second was ordered, we add the first to results and set its split variable to the common variable.
        /// 3, If both were ordered we skip them, if non of them are used we add both and set split variable to the second pattern.
        /// Usused patterns, those that couldnt be connected to any other pattern, are added at the end. 
        /// Their splitBy property remains set to Null, the same counts.
        /// </summary>
        /// <param name="parsedPatterns"> Parser Pattern from MatchVisitor </param>
        /// <returns> Ordered parsed patterns. </returns>
        private List<ParsedPattern> OrderParsedPatterns(List<ParsedPattern> parsedPatterns)
        {
            List<ParsedPattern> result = new List<ParsedPattern>();
            bool[] usedPatterns = new bool[parsedPatterns.Count];
            usedPatterns.Populate(false);

            // For each pattern in ParsedPatterns
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                var currentParsedPattern = parsedPatterns[i];
                // Take all following patterns
                for (int j = i + 1; j < parsedPatterns.Count; j++)
                {
                    var otherParsedPattern = parsedPatterns[j];
                    if (currentParsedPattern.TryFindEqualVariable(otherParsedPattern, out string varName))
                    {
                        // Both patterns are ordered -> add both of them to results.
                        if (!usedPatterns[i] && !usedPatterns[j])
                        {
                            usedPatterns[i] = true; usedPatterns[j] = true;
                            result.Add(parsedPatterns[i]); result.Add(parsedPatterns[j]);
                            parsedPatterns[j].splitBy = varName;
                        }
                        // The first one is ordered and the second one is not -> add second one to the results.
                        else if (usedPatterns[i] && !usedPatterns[j])
                        {
                            usedPatterns[j] = true;
                            result.Add(parsedPatterns[j]);
                            parsedPatterns[j].splitBy = varName;
                        }
                        // Both were ordered. 
                        else if (usedPatterns[i] && usedPatterns[j])
                        {
                            // Special case, Added two unconnected nodes but the first one is later connected by later one
                            if (parsedPatterns[i].splitBy == null) parsedPatterns[i].splitBy = varName;
                        } 
                        // The first one is not ordered and the second one is -> add the first one to the results.
                        else if (!usedPatterns[i] && usedPatterns[j])
                        {
                            usedPatterns[i] = true;
                            result.Add(parsedPatterns[i]);
                            parsedPatterns[i].splitBy = varName;
                        }
                    }
                }
            }

            // Add rest of unconnected patterns to results.
            for (int i = 0; i < usedPatterns.Length; i++)
            {
                if (usedPatterns[i] == false) result.Add(parsedPatterns[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates pattern chain used in a matcher.
        /// 
        /// </summary>
        /// <param name="patternNodes"> Parsed pattern </param>
        /// <param name="map"> Map to store info about veriables </param>
        /// <returns> Chain of base matched for search algorithm. </returns>
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
        /// Calls apply on match object.
        /// </summary>
        /// <param name="element"> Element to be tested. </param>
        /// <returns> True if the element can be applied, false if it cannot be applied. </returns>
        public bool Apply(Element element)
        {
            return this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex].Apply(element, this.Scope);
        }

        /// <summary>
        /// Prepares subsequent pattern. That is, increment pattern index and set current node index to 0;
        /// Algorithm must ensure that the method is not called after last pattern.
        /// </summary>
        public void PrepareNextSubPattern()
        {
            this.CurrentPatternIndex++;
            this.CurrentMatchNodeIndex = 0;
            this.OverAllIndex++;
        }

        /// <summary>
        /// Algorithm should ensure that the method is not called after first pattern.
        /// Prepares previous pattern, reduces pattern index and sets current node index to last node in the pattern.
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
        /// </summary>
        public void PrepareNextNode()
        {
            this.CurrentMatchNodeIndex++;
            this.OverAllIndex++;
        }

        /// <summary>
        /// Prepares previous node.
        /// If the current node is not anonymous, we need to reset variable inside the scope and
        /// also from the sideways scope. The unset method can be called even if the variable is not set,
        /// in that case the outcomes are none.
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
        /// Unsets the variable representing current match node.
        /// Both from scope and used elements.
        /// </summary>
        public void UnsetCurrentVariable()
        {
            this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex].UnsetVariable(this.Scope);
        }

        /// <summary>
        ///  Gets starting element of the current chain.
        /// </summary>
        /// <returns> Null if anonymous/first appearance else element from scope. </returns>
        public Element GetCurrentChainConnection()
        {
            return this.Patterns[this.CurrentPatternIndex][0].GetVariable(this.Scope);
        }

        /// <summary>
        /// Gets starting element of the next chain.
        /// This method is called only when there is another pattern.
        /// If the next chain contains a variable that was already used it returns it from the scope.
        /// </summary>
        /// <returns>Null if anonymous/first appearance else element from scope. </returns>
        public Element GetNextChainConnection()
        {
            return this.Patterns[this.CurrentPatternIndex + 1][0].GetVariable(this.Scope);
        }

        public bool isLastNodeInCurrentPattern()
        {
            return (this.Patterns[this.CurrentPatternIndex].Count - 1) == this.CurrentMatchNodeIndex ? true : false;
        }

        public bool isLastPattern()
        {
            return this.CurrentPatternIndex == (this.Patterns.Count - 1) ? true : false;
        }

       /// <summary>
       /// Returns edge type of a current match node.
       /// </summary>
        public Type GetMatchType()
        {
            return (this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex]).GetMatchType();
        }

        /// <summary>
        /// Shallow copy of a pattern.
        /// </summary>
        public IDFSPattern Clone()
        {
            var tmpPattern = new DFSPattern(this.Patterns, this.Scope.Length);
            return tmpPattern;
        }

        /// <summary>
        /// Returns variables that have been matched so far.
        /// </summary>
        public Element[] GetMatchedVariables()
        {
            return this.Scope;
        }

        #endregion PatternInterface
    }


}
