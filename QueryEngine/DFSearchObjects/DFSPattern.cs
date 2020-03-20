﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Basic interface for each pattern.
    /// </summary>
    interface IPattern
    {
        bool Apply(Element element);

        void PrepareNextSubPattern();
        void PreparePreviousSubPattern();

        void PrepareNextNode();
        void PreparePreviousNode();

        bool isLastNodeInCurrentPattern();
        bool isLastPattern();


        int GetIndexOfCurrentPattern();
        int GetIndexOfCurrentMatchNode();
        int GetOverAllIndex();

        int GetPatternCount();
        int GetCurrentPatternCount();

        int GetAllNodeCount();

    }

    /// <summary>
    /// Interface neccessary for each DFS pattern.
    /// </summary>
    interface IDFSPattern : IPattern
    {
        Element GetCurrentChainConnection();
        Element GetNextChainConnection();
        EdgeType GetEdgeType();
        void UnsetCurrentVariable();
        IDFSPattern Clone();
    }




    /// <summary>
    /// Class that implements basic DFS pattern.
    /// Creates it self from parsed pattern.
    /// Pattern is represented by the lists of matching nodes,
    /// also it remembers the state of the matched variables and state
    /// which nodes and chains should be matched.
    /// </summary>
    class DFSPattern : IDFSPattern
    {
        private List<List<DFSBaseMatch>> Patterns;
        private int CurrentPatternIndex;
        private int CurrentMatchNodeIndex;
        private int OverAllIndex;

        /// <summary>
        /// Map of variables that maps strings (names of variables) is map for the whole query.
        /// We need to ensure that the indexes when retrieved are the same. 
        /// Integer here is the positionOfRepeated variable (index in Map of variables.)
        /// </summary>
        private Dictionary<int, Element> Scope;

        /// <summary>
        /// We need two dictionaries to check if elements rep. other variables.
        /// Problem is that id of a vertex and id of a edge can be the same -> that why to dicts.
        /// </summary>
        private Dictionary<Element, bool> MatchedVarsVertices;
        private Dictionary<Element, bool> MatchedVarsEdges;

        private DFSPattern(List<List<DFSBaseMatch>> dFSBaseMatches)
        {
            this.Patterns = dFSBaseMatches;
            this.Scope = new Dictionary<int, Element>();
            this.MatchedVarsEdges = new Dictionary<Element, bool>();
            this.MatchedVarsVertices = new Dictionary<Element, bool>();
            this.CurrentMatchNodeIndex = 0;
            this.CurrentPatternIndex = 0;
            this.OverAllIndex = 0;
        }

        public DFSPattern(VariableMap map, List<ParsedPattern> parsedPatterns)
        {
            this.Patterns = new List<List<DFSBaseMatch>>();
            this.CreatePattern(parsedPatterns, map);

            this.Scope = new Dictionary<int, Element>();
            this.MatchedVarsEdges = new Dictionary<Element, bool>();
            this.MatchedVarsVertices = new Dictionary<Element, bool>();
            this.CurrentMatchNodeIndex = 0;
            this.CurrentPatternIndex = 0;
            this.OverAllIndex = 0;
        }

        /// <summary>
        /// Creates pattern from Parsed Pattern made by match visitor, also creates a map for variables
        /// during pattern matching.
        /// Given pattern is check for correctness and ordered so each connected patterns go after each 
        /// Then the resulting pattern is created. Patterns to be splited are splited into two based on split variable.
        /// For example: (a) -> (b) -> (c) splited by var. b == (b) <- (a) , (b) -> (c)
        /// </summary>
        /// <param name="parsedPatterns"> Pattern created by Match Visitor </param>
        /// <param name="variableMap"> Query map of variables (empty) </param>
        protected void CreatePattern(List<ParsedPattern> parsedPatterns, VariableMap variableMap)
        {
            var orderedPatterns = OrderParsedPatterns(parsedPatterns);


            Console.ReadLine();
            // For every Parsed Pattern
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                // Try to split it.
                var firstPart = orderedPatterns[i].SplitParsedPattern();

                // If the parsed pattern was splited
                // Add both parts into the real Pattern
                if (firstPart != null)
                {
                    this.Patterns.Add(CreateChain(firstPart.Pattern, variableMap));
                }
                this.Patterns.Add(CreateChain(orderedPatterns[i].Pattern, variableMap));

            }
        }

        /// <summary>
        /// Orders patterns so that each consecutive pattern can be connected with patterns before.
        /// Map used patterns in array of bools. If we found two patterns that have got same variable
        /// Check for if one of the pattern is used, if first is used and second not, we add second to results and mark split by firsts var.
        /// If first is not used and second is, we add first to result and mark its splitby by second variable.
        /// If both are used we skip them, if non of them are used we add both and set split by policy to the second pattern.
        /// Usused patterns, those that couldnt be connected to any other pattern are added at the end.
        /// Their splitBy property remains set to Null.
        /// </summary>
        /// <param name="parsedPatterns"> Parser Pattern from MatchVisitor</param>
        protected List<ParsedPattern> OrderParsedPatterns(List<ParsedPattern> parsedPatterns)
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
                        if (!usedPatterns[i] && !usedPatterns[j])
                        {
                            usedPatterns[i] = true; usedPatterns[j] = true;
                            result.Add(parsedPatterns[i]); result.Add(parsedPatterns[j]);
                            parsedPatterns[j].splitBy = varName;
                        }
                        else if (usedPatterns[i] && !usedPatterns[j])
                        {
                            usedPatterns[j] = true;
                            result.Add(parsedPatterns[j]);
                            parsedPatterns[j].splitBy = varName;
                        }
                        else if (usedPatterns[i] && usedPatterns[j])
                        {
                            // Special case, Added two unconnected nodes which the first one is later connected by later one
                            if (parsedPatterns[i].splitBy == null) parsedPatterns[i].splitBy = varName;
                        }
                        else if (!usedPatterns[i] && usedPatterns[j])
                        {
                            usedPatterns[i] = true;
                            result.Add(parsedPatterns[i]);
                            parsedPatterns[i].splitBy = varName;
                        }
                    }
                }
            }

            // Add rest of unconnected patterns 
            for (int i = 0; i < usedPatterns.Length; i++)
            {
                if (usedPatterns[i] == false) result.Add(parsedPatterns[i]);
            }

            return result;
        }


        #region PatternCreation
        /// <summary>
        /// Creates pattern chain used in searcher.
        /// Also sets map for query.
        /// </summary>
        /// <param name="patternNodes"> Parsed pattern </param>
        /// <param name="map"> Map to store info about veriables </param>
        /// <returns></returns>
        protected List<DFSBaseMatch> CreateChain(List<ParsedPatternNode> patternNodes, VariableMap map)
        {
            List<DFSBaseMatch> tmpChain = new List<DFSBaseMatch>();

            // For each parsed pattern node
            for (int i = 0; i < patternNodes.Count; i++)
            {
                var tmpNode = patternNodes[i];
                int index = -1;
                bool isFirst = true;

                // If it has not got a name, do not add it to map.
                if (tmpNode.name != null)
                {
                    // Try if the variable is inside a dictionary
                    if ((index = map.GetVariablePosition(tmpNode.name)) == -1)
                    {
                        // If it is not, Add it there with the proper type and index.
                        // Note: Table can be null
                        index = map.GetCount();
                        map.AddVariable(tmpNode.name, index, tmpNode.table);
                    }
                    else isFirst = false;
                }

                // Create match node and add it to the chain.
                tmpChain.Add(DFSBaseMatch.CreateDFSBaseMatch(tmpNode.edgeType, patternNodes[i], index, isFirst));
            }
            return tmpChain;
        }

        #endregion PatternCreation

        /// <summary>
        /// Calls apply on match object, based on the current object we choose which dict will be passed into the apply method.
        /// We know that the sequence is vertex - edge - vertex, that is to say, vertex positions is divisible by 2.
        /// </summary>
        /// <param name="element"> Element to be tested. </param>
        /// <returns> True if the element can be applied, false if it cannot be applied. </returns>
        public bool Apply(Element element)
        {
            if ((this.CurrentMatchNodeIndex % 2) == 0)
                return this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex].Apply(element, this.Scope, this.MatchedVarsVertices);
            else
                return this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex].Apply(element, this.Scope, this.MatchedVarsEdges);
        }

        /// <summary>
        /// Algorithm should ensure that the method is not called after last pattern.
        /// Prepares subsequent pattern. That is, increment pattern index and set current node index to 0;
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
            this.CurrentMatchNodeIndex = this.GetCurrentPatternCount() - 1;
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
            var tmpNode = this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex];
            if ((this.CurrentMatchNodeIndex % 2) == 1)
                tmpNode.UnsetVariable(this.Scope, this.MatchedVarsEdges);
            else tmpNode.UnsetVariable(this.Scope, this.MatchedVarsVertices);
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

        public int GetIndexOfCurrentPattern()
        {
            return this.CurrentPatternIndex;
        }

        public int GetIndexOfCurrentMatchNode()
        {
            return this.CurrentMatchNodeIndex;
        }

        public int GetPatternCount()
        {
            return this.Patterns.Count;
        }


        public int GetCurrentPatternCount()
        {
            return this.Patterns[this.CurrentPatternIndex].Count;
        }

        public int GetAllNodeCount()
        {
            int Count = 0;
            for (int i = 0; i < this.Patterns.Count; i++)
            {
                Count += this.Patterns[i].Count;
            }
            return Count;
        }

        public EdgeType GetEdgeType()
        {
            return ((DFSEdgeMatch)(this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex])).GetEdgeType();
        }

        public int GetOverAllIndex()
        {
            return this.OverAllIndex;
        }

        public IDFSPattern Clone()
        {
            var tmpPattern = new DFSPattern(this.Patterns);
            return tmpPattern;
        }
    }


}