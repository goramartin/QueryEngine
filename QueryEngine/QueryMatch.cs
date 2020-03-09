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
        public  IPatternMatcher Matcher;
        public  IPattern Pattern;

        /// <summary>
        /// Creates Match expression
        /// </summary>
        /// <param name="tokens"> Tokens to be parsed. (Expecting first token to be a Match token.)</param>
        /// <param name="graph"> Graph to be conduct a query on. </param>
        /// <param name="variableMap"> Empty map of variables. </param>
        public MatchObject(List<Token> tokens, VariableMap variableMap, Graph graph)
        {
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
            this.Pattern = MatchFactory.CreatePattern("DFS", "SIMPLE", variableMap, result);
            this.Matcher = MatchFactory.CreateMatcher("DFS", Pattern, graph);
       
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

        public IPattern GetPattern() => this.Pattern;
        public IPatternMatcher GetMatcher() => this.Matcher;
    }







    /// <summary>
    /// Class representing single step of pattern to match.
    /// Method apply returns true if the element can be added to final result.
    /// </summary>
    abstract class DFSBaseMatch
    {
        // The match is anonymous if it does not represent any variable.
        protected bool anonnymous;
        // Is true if the match object represents a variable that has it is first appereance.
        protected bool firstAppereance;
        // Represents index in scope if it is not anonymous.
        protected int positionOfRepeatedField;
        protected Table type;

        public DFSBaseMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.type = null;
            this.firstAppereance = true;
        }

        /// <summary>
        /// Constructor for each DFS Match object.
        /// </summary>
        /// <param name="node"> Node containing data of the match object. </param>
        /// <param name="indexInMap"> Index in the map of variables. (-1 if the the variable is anonymous.) </param>
        protected DFSBaseMatch(ParsedPatternNode node, int indexInMap, bool isFirst)
        {
            if (indexInMap != -1) this.anonnymous = false;
            else this.anonnymous = true;

            this.firstAppereance = isFirst;
            this.positionOfRepeatedField = indexInMap;
            this.type = node.table;
        }


        /// <summary>
        /// Gets an element that will be tested if it can be added to the result.
        /// </summary>
        /// <param name="element"> Element to be tested. </param>
        /// <param name="scope"> Scope of variables in search context. </param>
        /// <returns> True if element can be aplicable or false on refusal. </returns>
        public abstract bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used);


        /// <summary>
        /// Called by descendants. Checks conditions that are indifferent to the descendant type.
        /// </summary>
        /// <param name="element"> Elemented to be tested. </param>
        /// <param name="map"> Scope of variables in search context.</param>
        /// <param name="used"> Variables used in the search (dep. on if the match object is Edge or Vertex). </param>
        /// <returns>True if element can be aplicable or false on refusal.</returns>
        protected bool CheckCommonConditions(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            // Check type, comparing references to tables.
            if ((this.type != null) && (this.type != element.GetTable())) return false;

            // It is anonnymous, then it can match any vertex/edge.
            if (this.anonnymous) return true;
            else  // it is a variable 
            {
                // Check if any element occupies variable rep. by this match object.
                if (map.TryGetValue(this.positionOfRepeatedField, out Element tmpEl))
                {
                    // It contains el. 
                    // Check if the elemets are same.
                    if (tmpEl.GetID() != element.GetID()) return false;
                    else { /* Empty else -> returns true at the end */ }

                } else // The dict does not contain the element.
                {
                    // Check if the element is used for another variable.
                    if (used.ContainsKey(element)) return false;
                    // Add it to the map and to the used elements.
                    else
                    {
                        map.Add(this.positionOfRepeatedField, element);
                        used.Add(element, true);
                    }
                }
            } 

            return true;
        }

        public int GetPositionOfRepeatedField() => this.positionOfRepeatedField;
        public Table GetTable() => this.type;
        public bool IsAnonnymous() => this.anonnymous;

        /// <summary>
        /// Returns whether the variable represented by this match objects is seen for the first time.
        /// </summary>
        public bool IsFirstAppereance() => this.firstAppereance;

        /// <summary>
        /// Unsets variable from scope and used elements.
        /// </summary>
        /// <param name="map"> Scope of the search algorithm. </param>
        /// <param name="used"> Used elements (edges/vertices. </param>
        public void UnsetVariable(Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (this.firstAppereance && !this.anonnymous)
            {
                if (map.TryGetValue(this.positionOfRepeatedField, out Element tmpElement))
                {
                    map.Remove(this.positionOfRepeatedField);
                    used.Remove(tmpElement);
                }
            }
        }

        /// <summary>
        /// Gets element corresponding to this match object.
        /// </summary>
        /// <param name="map"> Scope of the search algorithm. </param>
        /// <returns> Null if no element is used, else element of this match object. </returns>
        public Element GetVariable(Dictionary<int, Element> map)
        {
            if (this.anonnymous) return null;
            else
            {
                if (this.firstAppereance) return null;
                else return map[this.positionOfRepeatedField];
            }
        }
        
    }

    class DFSVertexMatch : DFSBaseMatch
    {
        public DFSVertexMatch() : base()
        { }
        
        public DFSVertexMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is Vertex)) return false;
            else return CheckCommonConditions(element, map, used);
        }

    }

    abstract class DFSEdgeMatch : DFSBaseMatch
    {
        public DFSEdgeMatch() : base()
        { }

        public DFSEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public abstract EdgeType GetEdgeType();

        /// <summary>
        /// Sets type of last matched edge.
        /// Must be called only on AnyEdge type.
        /// </summary>
        public abstract void SetLastEdgeType(EdgeType type);


        /// <summary>
        /// Returns type of last matched edge.
        /// Must be called only on AnyEdge type.
        /// </summary>
        public abstract EdgeType GetLastEdgeType();

    }

    class DFSInEdgeMatch : DFSEdgeMatch
    {
        public DFSInEdgeMatch() : base()
        { }
        public DFSInEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.InEdge;
        public override void SetLastEdgeType(EdgeType type) { /* Empty body */ }
        public override EdgeType GetLastEdgeType() => EdgeType.InEdge;
      
        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is InEdge)) return false;
            else return CheckCommonConditions(element, map, used);
        }

    }
    class DFSOutEdgeMatch : DFSEdgeMatch
    {
        public DFSOutEdgeMatch() : base()
        { }
        public DFSOutEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.OutEdge;
        public override void SetLastEdgeType(EdgeType type) { /* Empty body */ }
        public override EdgeType GetLastEdgeType() => EdgeType.OutEdge;

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is OutEdge)) return false;
            else return CheckCommonConditions(element, map, used);
        }

    }

    class DFSAnyEdgeMatch : DFSEdgeMatch
    {
        protected EdgeType lastEdgeType;

        public DFSAnyEdgeMatch() : base()
        { }
        public DFSAnyEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.AnyEdge;
        public override void SetLastEdgeType(EdgeType type) => this.lastEdgeType = type;
        public override EdgeType GetLastEdgeType() => this.lastEdgeType;

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is Edge)) return false;
            else return CheckCommonConditions(element, map, used);
        }
    }



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

        EdgeType GetLastEdgeType();
        void SetLastEdgeType(EdgeType type);

        void UnsetCurrentVariable();
        void UnsetLastEdgeType();
    }


    /// <summary>
    /// Class that implements basic DFS pattern.
    /// Creates it self from parsed pattern.
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

        #region PatternCreation

        /// <summary>
        /// Creates pattern from Parsed Pattern made by match visitor, also creates a map for variables
        /// during pattern matching.
        /// Given pattern is check for correctness and ordered so each connected patterns go after each 
        /// Then the resulting pattern is created. Patterns to be splited are splited into two based on split variable.
        /// For example: (a) -> (b) -> (c) splited by var. b == (b) <- (a) , (b) -> (c)
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
                // Try to split it.
                var firstPart = orderedPatterns[i].SplitParsedPattern();       
                
                // If the parsed pattern was splited
                // Add both parts into the real Pattern
                if ( firstPart != null)
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

        /// <summary>
        /// Creates pattern chain used in searcher.
        /// Also sets map for query.
        /// </summary>
        /// <param name="patternNodes"> Parsed pattern </param>
        /// <param name="map"> Map to store info about veriables </param>
        /// <returns></returns>
        private List<DFSBaseMatch> CreateChain(List<ParsedPatternNode> patternNodes, VariableMap map) 
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
                tmpChain.Add(CreateDFSBaseMatch(tmpNode.edgeType, patternNodes[i], index, isFirst));
            }
            return tmpChain;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"> Type of match node</param>
        /// <param name="node"> Prototype of the node </param>
        /// <param name="indexInMap"> Index of its variable in scope </param>
        /// <returns></returns>
        private DFSBaseMatch CreateDFSBaseMatch(EdgeType edgeType, ParsedPatternNode node, int indexInMap, bool isFirst)
        {
            switch (edgeType)
            {
                case EdgeType.NotEdge:
                    return new DFSVertexMatch(node, indexInMap, isFirst);
                case EdgeType.InEdge:
                    return new DFSInEdgeMatch(node, indexInMap, isFirst);
                case EdgeType.OutEdge:
                    return new DFSOutEdgeMatch(node, indexInMap, isFirst);
                case EdgeType.AnyEdge:
                    return new DFSAnyEdgeMatch(node, indexInMap, isFirst);
                default:
                    throw new ArgumentException($"{this.GetType()} Trying to create Match type that does not exit.");
            }
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
        /// </summary>
        public void PrepareNextSubPattern()
        {
            this.CurrentPatternIndex++;
            this.CurrentMatchNodeIndex = 0;
            this.OverAllIndex++;
        }

        /// <summary>
        /// Algorithm should ensure that the method is not called after first pattern.
        /// </summary>
        public void PreparePreviousSubPattern()
        {
            this.CurrentPatternIndex--;
            this.CurrentMatchNodeIndex = this.GetCurrentPatternCount() - 1;
            this.OverAllIndex--;
        }

        /// <summary>
        /// The algorithm should ensure that the method is not called after the last match node.
        /// </summary>
        public void PrepareNextNode()
        {
            this.CurrentMatchNodeIndex++;
            this.OverAllIndex++;
        }

        /// <summary>
        /// Prepares previous node.
        /// If the current node is not anonymous, we need to reset variable inside the scope and
        /// also from the sideways scope. 
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
        public void UnsetCurrentVariable()
        {
            var tmpNode = this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex];
            if ((this.CurrentMatchNodeIndex % 2) == 1)
                tmpNode.UnsetVariable(this.Scope, this.MatchedVarsEdges);
            else tmpNode.UnsetVariable(this.Scope, this.MatchedVarsVertices);
        }
        public void UnsetLastEdgeType()
        {
            ((DFSEdgeMatch)this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex]).SetLastEdgeType(EdgeType.NotEdge);
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
            return this.Patterns[this.CurrentPatternIndex+1][0].GetVariable(this.Scope);
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

        public EdgeType GetLastEdgeType()
        {
            return ((DFSEdgeMatch)(this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex])).GetLastEdgeType();
        }

        public void SetLastEdgeType(EdgeType type)
        {
           ((DFSEdgeMatch)(this.Patterns[this.CurrentPatternIndex][this.CurrentMatchNodeIndex])).SetLastEdgeType(type);
        }

        public int GetOverAllIndex()
        {
            return this.OverAllIndex;
        }

    }






    interface IPatternMatcher
    {
        void Search();
    }

    /// <summary>
    /// Class represents DFS search that accepts patterns with IDFSPattern interface
    /// </summary>
    class DFSPatternMatcher : IPatternMatcher
    {
        Graph graph;
        IDFSPattern pattern;
        Element[] result;
        bool processingVertex;
        
        public DFSPatternMatcher(IDFSPattern pattern, Graph graph)
        {
            this.graph = graph;
            this.result = new Element[pattern.GetAllNodeCount()];
            this.pattern = pattern;
        }


        public void Search()
        {
            int[] lastUsedIndeces = new int[pattern.GetPatternCount()];
            lastUsedIndeces.Populate(-1);

            int lastUsedIndex = -1;

            while (true)
            {
                // -1 meaning that next conjunction will start from the beginning. 
                if (lastUsedIndex == -1) lastUsedIndex = DFSStartOfCunjunction(0, false);
                // Else it uses last used index in that conjunction.
                else lastUsedIndex = DFSStartOfCunjunction(lastUsedIndex, true);
                
                // - 1 one finished whole pattern -> going down or we finished searching
                if (lastUsedIndex == -1)
                {
                    if (pattern.GetIndexOfCurrentPattern() == 0) break; // end
                    pattern.PreparePreviousSubPattern();
                    lastUsedIndex = lastUsedIndeces[pattern.GetIndexOfCurrentPattern()];
                }
                else // pick next pattern
                {
                    lastUsedIndeces[pattern.GetIndexOfCurrentPattern()] = lastUsedIndex;
                    pattern.PrepareNextSubPattern();
                    lastUsedIndex = -1;
                }
            }
        }

        /// <summary>
        /// Initiates iteration over one connected pattern.
        /// </summary>
        /// <param name="lastIndex"> Last index from last iteration. </param>
        /// <param name="cameFromUp"> If we came from a different conjunction. </param>
        /// <returns> Last used index. </returns>
        public int DFSStartOfCunjunction(int lastIndex, bool cameFromUp)
        {
            var vertices = graph.GetAllVertices();
            for (int i = lastIndex; i < vertices.Count; i++)
            {
                processingVertex = true;
                Element nextElement = vertices[i];
                if (cameFromUp)
                {
                    nextElement = null;
                    cameFromUp = false;
                }
                
                // Iteration over the connected chains.
                while (true)
                {
                    var canContinue = DFSMainLoop(nextElement);
                    
                    // If there is more chains
                    if (canContinue) 
                    {
                        // If the new chain is not connected, that means there is another conjunction.
                        // Otherwise we take the element from the connection and start new dfs chain with that element.
                        if ((nextElement = pattern.GetNextChainConnection()) != null) 
                        {
                            pattern.PrepareNextSubPattern();
                            continue; 
                        }
                        else return i;
                    } else 
                    {
                        // If there are no more chains or we are simply returning
                        // If we are at the starting chain of conjunction we let main for loop pick next starting vertex.
                        if ((pattern.GetCurrentChainConnection()) == null) break;
                        else
                        {
                            // If we are connected to the before pattern, we will initiate returning.
                            pattern.PreparePreviousSubPattern();
                            nextElement = null;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Main loop of the dfs, always consumes one sub pattern from pattern.
        /// When it returns we expect the sub pattern to be empty of full.
        /// </summary>
        /// <param name="nextElement"> Element to start on. </param>
        /// <returns> True if there is another pattern, False if it is returning. </returns>
        private bool DFSMainLoop(Element nextElement)
        {
            while (true)
            {
                // Try to apply the new element to the pattern.
                bool success = pattern.Apply(nextElement);
                if (success)
                {
                    // If it is the last node in the pattern, we check if it is the last pattern.
                    AddToResult(nextElement);
                    if (pattern.isLastNodeInCurrentPattern())
                    {
                        if (pattern.isLastPattern())
                        {
                            // Setting null here makes it to fail on it and it is forced to dfs back.
                            result.Print();
                            nextElement = null;
                            continue;
                        }
                        else return true; // The i is set when we return from the above pattern so we can start iteration from this point.
                    }
                    pattern.PrepareNextNode();
                    nextElement = DoDFSForward(nextElement, null);
                }
                else
                {
                    nextElement = DoDFSBack(nextElement);
                    if (pattern.GetIndexOfCurrentMatchNode() <= 0) // now it should never happen-1 one if it fails on matching the first vertex.
                    {
                        this.ClearCurrentFromResult();
                        pattern.PreparePreviousNode();
                        break;
                    } // Continues in the main cycle.
                }
            }
            return false;
        }




        /// <summary>
        /// Method seaches for the next element to match.
        /// If the last matched element is vertex we look for an edge.
        /// If the last matched element is edge we just take the end vertex.
        /// Last used edge is filled only when calling from dfs back.
        /// In this case we do not add anything to the result.
        /// </summary>
        /// <param name="lastUsedElement"> Last matched Element. </param>
        /// <param name="lastUsedEdge"> Last matched edge. </param>
        /// <returns> Next element that will be tried to applied. </returns>
        private Element DoDFSForward(Element lastUsedElement, Edge lastUsedEdge)
        {
            if (processingVertex)
            {
                EdgeType edgeType = pattern.GetEdgeType();
                Edge nextEdge = FindNextEdge(edgeType, (Vertex)lastUsedElement, lastUsedEdge);

                processingVertex = false;
                return nextEdge;
            }
            else
            {
                processingVertex = true;
                return ((Edge)lastUsedElement).endVertex;
            }
        }


        /// <summary>
        /// Processing Vertex:
        /// We are returning from the dfs.
        /// When processing the vertex, we failed to add the vertex to the pattern, 
        /// that means we need to go down in the pattern and also remove the edge we came with to the vertex. 
        /// In order to do so, we will return null, next loop in algorithm fails on adding edge, so the edge gets removed.
        /// 
        /// Processing Edge:
        /// When processing edge, we get the last used edge from the result, remove it from results.
        /// (Note there can be no edge, eg: we failed to add one at all.)
        /// We take the edge (null or normal edge) and try to do dfs from the vertex the edge started from 
        /// If it returns a new edge we can continue trying to apply the edge on the same index in pattern.
        /// If it is null we need to remove also the vertex because there are no more available edges from this vertex.
        /// In order to do that we go down in pattern and return null, so the algorithm fail 
        /// on adding vertex so it jumps here again.
        /// </summary>
        /// <param name="lastElement"> Last element we failed on. </param>
        /// <returns>  Element to continue in the search. </returns>
        private Element DoDFSBack(Element lastElement)
        {
            if (processingVertex)
            {
                ClearCurrentFromResult();
                pattern.PreparePreviousNode();
                processingVertex = false;
                return null;
            }
            else
            {
                // Take the edge on the current position. (Edge that was matched before, can be null if no edge was there.)
                Element lastUsedEdgeInResult = (Edge)result[pattern.GetOverAllIndex()];
                
                // lastElement is null only when we are returning from the removed vertex, we take the last used edge in the result.
                // Else we always use the newest edge we failed on. 
                if (lastElement == null) lastElement = lastUsedEdgeInResult;
                
                // Clears the last used edge, or does nothing.
                // I need to clean the scope so that i can apply next possible edge
                ClearCurrentFromResult();
                pattern.UnsetCurrentVariable();


                // Try to find new edge from the last vertex.
                processingVertex = true; //To jump into dfs.
                Element nextElement =
                    DoDFSForward((Vertex)result[pattern.GetOverAllIndex()-1], (Edge)lastElement);
                
                // If no edge was found, we want to remove also the last vertex. (because we consumed all of his edges)
                // Returning null in this position removes the vertex in the next cycle of the main algorithm.
                // Else we continue in searching with the new edge on the same index of the match node.
                if (nextElement == null)
                {
                    pattern.UnsetLastEdgeType();
                    pattern.PreparePreviousNode();
                    processingVertex = true;
                    return null;
                } else return nextElement;
            }
        }

        /// <summary>
        /// Finds next edge based on given type.
        /// </summary>
        /// <param name="edgeType"> Type of edge. </param>
        /// <param name="vertex"> Vertex that the edge is coming from. </param>
        /// <param name="lastUsedEdge"> Possibly, last used edge of the vertex. </param>
        /// <returns> Next edge. </returns>
        private Edge FindNextEdge(EdgeType edgeType, Vertex lastUsedVertex, Edge lastUsedEdge)
        {
            if (edgeType == EdgeType.InEdge) return FindInEdge(lastUsedVertex, lastUsedEdge);
            else if (edgeType == EdgeType.OutEdge) return FindOutEdge(lastUsedVertex, lastUsedEdge);
            else return FindAnyEdge(lastUsedVertex, lastUsedEdge);
        }


        /// <summary>
        /// Returns a next inward edge to be processed of the given vertex.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next inward edge of the vertex. </returns>
        private Edge FindInEdge(Vertex vertex, Edge lastUsedEdge)
        {
            vertex.GetRangeOfInEdges(out int start, out int end);
            return GetNextEdge<InEdge>(start, end, graph.GetAllInEdges(), lastUsedEdge);
        }

        /// <summary>
        /// Returns a next outward edge to be processed of the given vertex.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next outward edge of the vertex. </returns>
        private Edge FindOutEdge(Vertex vertex, Edge lastUsedEdge)
        {
            vertex.GetRangeOfOutEdges(out int start, out int end);
            return GetNextEdge<OutEdge>(start, end, graph.GetAllOutEdges(), lastUsedEdge);
        }

        /// <summary>
        /// Returns a next edge to be processed of the given vertex.
        /// Fixed erros when returning to from another pattern caused using different edge types.
        /// Notice that this method is called only when matching type Any of the edge.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched for next possible. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next edge of the vertex. </returns>
        private Edge FindAnyEdge(Vertex vertex, Edge lastUsedEdge)
        {
            Edge nextEdge = null;
            // Searching can start newly -> last edge type == not edge (after reseting) or we are processing in edge.
            if (pattern.GetLastEdgeType() == EdgeType.NotEdge || pattern.GetLastEdgeType() == EdgeType.InEdge)
            {
                nextEdge = FindInEdge(vertex, lastUsedEdge);
                if (nextEdge == null)
                {
                    lastUsedEdge = null;
                    pattern.SetLastEdgeType(EdgeType.OutEdge);
                }
                else pattern.SetLastEdgeType(EdgeType.InEdge);
            }

            // After we tried using inEdges we look for out edges.
            if (pattern.GetLastEdgeType() == EdgeType.OutEdge) nextEdge = FindOutEdge(vertex, lastUsedEdge);

            //After we searched all out edges, the state should be reset and lastedge type set to not edge 
            return nextEdge;
        }

        /// <summary>
        /// Returns next edge to process. We expect the the last used edge is from the list.
        /// </summary>
        /// <param name="start"> Index of a first edge of the processed vertex. -1 that the vertex does not have edges. </param>
        /// <param name="end"> Index of a last edge of the processed vertex. </param>
        /// <param name="edges"> All edges (in or out) of the graph. </param>
        /// <param name="lastUsedEdge"> Last processed edge of the processed vertex. Null signifies that no edge of the vertex was processed. </param>
        /// <returns> Next edge.  </returns>
        /// <typeparam name="T"> Type of edge that the list is filled with. </typeparam>
        private Edge GetNextEdge<T>(int start, int end, List<T> edges, Edge lastUsedEdge) where T:Edge
        {
            // The processed vertex have not got edges.
            if (start == -1) return null;
            // No edge was used from the processed vertex -> pick the first one.
            else if (lastUsedEdge == null) return edges[start];
            // The Last processed Edge was the last edge of the vertex -> can not pick more edges.
            else if (end - 1 == lastUsedEdge.positionInList) return null;
            // There are more non processed edges of the vertex -> pick the following one from the edge list.
            else return edges[lastUsedEdge.positionInList + 1];
        }

        /// <summary>
        /// Adds element to the result.
        /// </summary>
        /// <param name="element">Element to be added to result.</param>
        private void AddToResult(Element element)
        {
            result[pattern.GetOverAllIndex()] = element;
        }

        /// <summary>
        /// Removes the last element from the result.
        /// </summary>
        private void ClearCurrentFromResult()
        {
            result[pattern.GetOverAllIndex()] = null;
        }   
    }
    





    /// <summary>
    /// Class includes register of all the Matchers and their coresponding patterns.
    //  Enables to create instance of a Matcher/Pattern based on a string token.
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
            RegisterMatcher("DFS", typeof(DFSPatternMatcher));
            RegisterPatternToMatcher("DFS", "SIMPLE", typeof(DFSPattern));
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

        public static IPatternMatcher CreateMatcher(string matcher, IPattern pattern, Graph graph)
        {
            if (!MatcherRegistry.ContainsKey(matcher))
                throw new ArgumentException("MatchFactory: Matcher Token not found.");

            Type matcherType = null;
            if (MatcherRegistry.TryGetValue(matcher, out matcherType))
            {
                return (IPatternMatcher)Activator.CreateInstance(matcherType, pattern, graph);
            }
            else throw new ArgumentException("MatchFactory: Failed to load type from Matcher registry.");
        }

        public static IPattern CreatePattern(string matcher, string pattern, VariableMap map, List<ParsedPattern> parsedPatterns)
        {
            if (MatcherPatternRegistry.TryGetValue(matcher, out Dictionary<string, Type> pDict))
            {
                if (pDict.TryGetValue(pattern, out Type patternType)) 
                 return (IPattern)Activator.CreateInstance(patternType, map, parsedPatterns);
                else throw new ArgumentException("MatchFactory: Failed to load type from  Pattern registry.");
            }
            else throw new ArgumentException("MatchFactory: Failed to load type from  Pattern registry.");
        }
    }
}


