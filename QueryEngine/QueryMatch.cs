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
        private IPatternMatcher Matcher;
        private IPattern Pattern;

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
            this.Matcher = MatchFactory.CreateMatcher("DFS");
            this.Pattern = MatchFactory.CreatePattern("DFS", "SIMPLE", variableMap, result);
       
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
    }







    /// <summary>
    /// Class representing single step of pattern to match.
    /// Method apply returns true if the element can be added to final result.
    /// </summary>
    abstract class DFSBaseMatch
    {
        protected bool anonnymous;
        protected bool repeatedVariable;
        protected int positionOfRepeatedField;
        protected Table type;

        public DFSBaseMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        /// <summary>
        /// Constructor for each DFS Match object.
        /// </summary>
        /// <param name="node"> Node containing data of the match object. </param>
        /// <param name="indexInMap"> Index in the map of variables. (-1 if the the variable is anonymous.) </param>
        protected DFSBaseMatch(ParsedPatternNode node, int indexInMap)
        {
            if (indexInMap != -1)
            {
                this.repeatedVariable = true;
                this.anonnymous = false;
            }
            else
            {
                this.repeatedVariable = false;
                this.anonnymous = true;
            }
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

            // It is repetition of variable before
            if (this.repeatedVariable)
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

        public void SetAnnonymous(bool b) => this.anonnymous = b;
        public bool IsAnonnymous() => this.anonnymous;
        
        public void SetRepeated(bool b) => this.repeatedVariable = b;
        public void SetPositionOfRepeatedField(int p) => this.positionOfRepeatedField = p;
        public int GetPositionOfRepeatedField() => this.positionOfRepeatedField;
        public bool IsRepeated() => this.repeatedVariable;
        
        /// <summary>
        /// Returns whether the variable representing this match node is empty or occupied.
        /// Notice that the function is called only if we have valid index -> never if the index is -1.
        /// </summary>
        /// <param name="scope"> Scope of searcher </param>
        /// <returns> True on empty </returns>
        public bool IsFirstAppereance(Dictionary<int, Element> map)
        {
            return !map.ContainsKey(this.positionOfRepeatedField);
        }

        public void SetType(Table table) => this.type = table;
        public Table GetTable() => this.type;
        

    }

    class DFSVertexMatch : DFSBaseMatch
    {
        public DFSVertexMatch() : base()
        { }
        
        public DFSVertexMatch(ParsedPatternNode node, int indexInMap) : base(node, indexInMap)
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

        public DFSEdgeMatch(ParsedPatternNode node, int indexInMap) : base(node, indexInMap)
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
        public DFSInEdgeMatch(ParsedPatternNode node, int indexInMap) : base(node, indexInMap)
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
        public DFSOutEdgeMatch(ParsedPatternNode node, int indexInMap) : base(node, indexInMap)
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
        public DFSAnyEdgeMatch(ParsedPatternNode node, int indexInMap) : base(node, indexInMap)
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
        void PrepareNext();
        void PreparePrevious();

        bool isLastNodeInCurrentPattern();
        bool isLastPattern();


        int GetIndexOfCurrentPattern();
        int GetIndexOfCurrentMatchNode();

        int GetPatternCount();
        int GetCurrentPatternCount();

        int GetAllNodeCount();

    }

    /// <summary>
    /// Interface neccessary for each DFS pattern.
    /// </summary>
    interface IDFSPattern : IPattern
    {
        Element GetConnection();
        EdgeType GetEdgeType();

        EdgeType GetLastEdgeType();
        void SetLastEdgeType(EdgeType type);
    }


    /// <summary>
    /// Class that implements basic DFS pattern.
    /// Creates it self from parsed pattern.
    /// </summary>
    class DFSPattern : IDFSPattern
    {
        private List<List<DFSBaseMatch>> Patterns;
        private int CurrentPattern;
        private int CurrentMatchNode;

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
            this.CurrentMatchNode = 0;
            this.CurrentPattern = 0;
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
                
                // If it has not got a name, do not add it to map.
                if (tmpNode.name != null)
                {
                    // Try if the variable is inside a dictionary
                    if ( (index = map.GetVariablePosition(tmpNode.name)) == -1)
                    {
                        // If it is not, Add it there with the proper type and index.
                        // Note: Table can be null
                        index = map.GetCount();
                        map.AddVariable(tmpNode.name, index, tmpNode.table);
                    }
                }

                // Create match node and add it to the chain.
                tmpChain.Add(CreateDFSBaseMatch(tmpNode.edgeType, patternNodes[i], index));
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
        private DFSBaseMatch CreateDFSBaseMatch(EdgeType edgeType, ParsedPatternNode node, int indexInMap)
        {
            switch (edgeType)
            {
                case EdgeType.NotEdge:
                    return new DFSVertexMatch(node, indexInMap);
                case EdgeType.InEdge:
                    return new DFSAnyEdgeMatch(node, indexInMap);
                case EdgeType.OutEdge:
                    return new DFSOutEdgeMatch(node, indexInMap);
                case EdgeType.AnyEdge:
                    return new DFSAnyEdgeMatch(node, indexInMap);
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
            if ((this.CurrentMatchNode % 2) == 0)
               return this.Patterns[this.CurrentPattern][this.CurrentMatchNode].Apply(element, this.Scope, this.MatchedVarsVertices);
            else
                return this.Patterns[this.CurrentPattern][this.CurrentMatchNode].Apply(element, this.Scope, this.MatchedVarsEdges);
        }
        public void PrepareNext()
        {
            // sets
            throw new NotImplementedException();
        }
        public void PreparePrevious()
        {
            // unsets
            throw new NotImplementedException();
        }
        public Element GetConnection()
        {
            // get if the node is connected 
            throw new NotImplementedException();
        }



        public bool isLastNodeInCurrentPattern()
        {
            return (this.Patterns[this.CurrentPattern].Count - 1) == this.CurrentMatchNode ? true : false;
        }

        public bool isLastPattern()
        {
            return this.CurrentPattern == (this.Patterns.Count - 1) ? true : false;
        }

        public int GetIndexOfCurrentPattern()
        {
            return this.CurrentPattern;
        }

        public int GetIndexOfCurrentMatchNode()
        {
            return this.CurrentMatchNode;
        }

        public int GetPatternCount()
        {
            return this.Patterns.Count;
        }


        public int GetCurrentPatternCount()
        {
            return this.Patterns[this.CurrentPattern].Count;
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
            return ((DFSEdgeMatch)(this.Patterns[this.CurrentPattern][this.CurrentMatchNode])).GetEdgeType();
        }

        public EdgeType GetLastEdgeType()
        {
            return ((DFSEdgeMatch)(this.Patterns[this.CurrentPattern][this.CurrentMatchNode])).GetLastEdgeType();
        }

        public void SetLastEdgeType(EdgeType type)
        {
           ((DFSEdgeMatch)(this.Patterns[this.CurrentPattern][this.CurrentMatchNode])).SetLastEdgeType(type);
        }

    }






    interface IPatternMatcher
    {
        void Search();
    }
    class DFSPatternMatcher : IPatternMatcher
    {
        public void Search() { }

        /*
        Graph graph;
        List<List<BaseMatch>> pattern;
        List<BaseMatch> currentPattern;
        Element[] result;
        bool processingVertex;
        bool processingInEdge; //valid only when any edge is wanted 
        int patternIndex;
        int currentPatternIndex;

        /*
        Graph graph;
        IPattern pattern;
        List<Element> result;
        bool processingVertex;
        bool processingInEdge; //valid only when any edge is wanted 
        


        public DFSPatternMatcher(List<List<BaseMatch>> p, Graph g)
        {
            this.graph = g;
            this.result = new Element[p.GetCount()];
            this.pattern = p;
        }

        public void Search()
        {
            patternIndex = 0;
            currentPattern = pattern[patternIndex];
            int[] lastUsedVertices = new int[pattern.Count - 1];
            int lastUsedVertex = -1;
            while (true)
            {
                if (lastUsedVertex == -1) lastUsedVertex = DFS(0, false);
                else lastUsedVertex = DFS(lastUsedVertex, true);
                if (lastUsedVertex == -1)
                {
                    if (patternIndex == 0) break;
                    patternIndex--;
                    lastUsedVertex = lastUsedVertices[patternIndex];
                    currentPattern = pattern[patternIndex];
                }
                else
                {
                    lastUsedVertices[patternIndex] = lastUsedVertex;
                    patternIndex++;
                    currentPattern = pattern[patternIndex];
                    lastUsedVertex = -1;
                }
            }
        }

        public int DFS(int position, bool cameFromUp)
        {
            var vertices = graph.GetAllVertices();
            for (int i = position; i < vertices.Count; i++)
            {
                processingVertex = true;
                processingInEdge = true;
                Element nextElement = vertices[i];
                currentPatternIndex = 0;
                if (cameFromUp)
                {
                    nextElement = null;
                    currentPatternIndex = currentPattern.Count - 1;
                    cameFromUp = false;
                }
                while (true)
                {
                    bool success = currentPattern[currentPatternIndex].Apply(nextElement, pattern, result);
                    if (success)
                    {
                        AddToResult(nextElement, currentPatternIndex);//to do
                        if ((currentPattern.Count - 1) == currentPatternIndex) //last position in current pattern
                        {
                            if (patternIndex == pattern.Count - 1) //last pattern from patterns
                            {
                                result.Print();
                                nextElement = null;
                                continue;
                            }
                            else return i;
                        }
                        currentPatternIndex++;
                        nextElement = DoDFSForward(nextElement, null);
                    }
                    else
                    {
                        nextElement = DoDFSBack(nextElement);
                        if (currentPatternIndex <= 0) break;
                    }
                }
            }
            return -1;
        }

        private Element DoDFSForward(Element lastElement, Edge lastUsedEdge)
        {
            if (processingVertex)
            {
                EdgeType edgeType = ((EdgeMatch)currentPattern[currentPatternIndex]).GetEdgeType();

                Element nextElement = null;
                if (edgeType == EdgeType.InEdge) nextElement = ProcessInEdge((Vertex)lastElement, lastUsedEdge);
                else if (edgeType == EdgeType.OutEdge) nextElement = ProcessOutEdge((Vertex)lastElement, lastUsedEdge);
                else nextElement = ProcessAnyEdge((Vertex)lastElement, lastUsedEdge);

                processingVertex = false;
                return nextElement;
            }
            else
            {
                processingVertex = true;
                return ((Edge)lastElement).endVertex;
            }
        }

        //When processing the vertex, we failed to add the vertex, that means we need to go down in the pattern,
        //but also remove the edge we came with to the vertex. So we return null, next loop in algorithm fails on adding edge, so the edge gets removed.
        //When processing edge, we get the last used edge, remove it from results (Note there can be no edge). 
        //We try to do dfs from the vertex the edge started from with the edge we got from results before. If it returns edge we can continue 
        //trying to apply edge on the same index in pattern. If it is null we need to remove the vertex because there are no more available edges.
        //In order to do that we go down in pattern and return null, so the algorithm fail on adding vertex so it jumps here again.
        private Element DoDFSBack(Element element)
        {
            if (processingVertex)
            {
                RemoveFromResult(currentPatternIndex);
                currentPatternIndex--;
                processingVertex = false;
                return null;
            }
            else
            {
                Edge lastUsedEdge = (Edge)result[GetAbsolutePosition(currentPatternIndex)];
                if (element == null) element = lastUsedEdge;
                RemoveFromResult(currentPatternIndex);

                processingVertex = true; //To jump into dfs.
                Element nextElement =
                    DoDFSForward(result[GetAbsolutePosition(currentPatternIndex) - 1], (Edge)element);
                if (nextElement == null)
                {
                    currentPatternIndex--;
                    processingVertex = true;
                }
                return nextElement;
            }
        }

        /// <summary>
        /// Returns a next inward edge to be processed of the given vertex.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next inward edge of the vertex. </returns>
        private Element ProcessInEdge(Vertex vertex, Element lastUsedEdge)
        {
            vertex.GetRangeOfInEdges(out int start, out int end);
            return FindNextEdge<InEdge>(start, end, graph.GetAllInEdges(), lastUsedEdge);
        }

        /// <summary>
        /// Returns a next outward edge to be processed of the given vertex.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next outward edge of the vertex. </returns>
        private Element ProcessOutEdge(Vertex vertex, Element lastUsedEdge)
        {
            vertex.GetRangeOfOutEdges(out int start, out int end);
            return FindNextEdge<OutEdge>(start, end, graph.GetAllOutEdges(), lastUsedEdge);
        }

        /// <summary>
        /// Returns a next edge to be processed of the given vertex.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next edge of the vertex. </returns>
        private Element ProcessAnyEdge(Vertex vertex, Element lastUsedEdge)
        {

            // to do Error when we jump to the next pattern and then back we always try to go into in edges with the edge from the other list 
            // fix each edge should know it is type
            Element nextEdge = null;
            if (processingInEdge)
            {
                nextEdge = ProcessInEdge(vertex, lastUsedEdge);
                if (nextEdge == null) { 
                    lastUsedEdge = null;
                    processingInEdge = false; 
                }
            }
            if (!processingInEdge) nextEdge = ProcessOutEdge(vertex, lastUsedEdge);
            return nextEdge;

        }

        /// <summary>
        /// Returns next edge to process. We expect the the last used edge is from the list.
        /// </summary>
        /// <param name="start"> Index of a first edge of the processed vertex. -1 that the vertex does not have edges. </param>
        /// <param name="end"> Index of a last edge of the processed vertex. </param>
        /// <param name="edges"> All edges (in or out) of the graph. </param>
        /// <param name="lastUsedEdge"> Last processed edge of the processed vertex. Null signifies that no edge of the vertex was processed. </param>
        /// <returns></returns>
        private Element FindNextEdge<T>(int start, int end, List<T> edges, Element lastUsedEdge) where T:Edge
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









        private void AddToResult(Element element, int index)
        {
            index = GetAbsolutePosition(index);
            this.result[index] = element;
        }
        private void RemoveFromResult(int index)
        {
            index = GetAbsolutePosition(index);
            this.result[index] = null;
        }
        private int GetAbsolutePosition(int index)
        {
            for (int i = 0; i < patternIndex; i++)
                index += pattern[i].Count;
            return index;
        }
    */
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

        public static IPatternMatcher CreateMatcher(string matcher)
        {
            if (!MatcherRegistry.ContainsKey(matcher))
                throw new ArgumentException("MatchFactory: Matcher Token not found.");

            Type matcherType = null;
            if (MatcherRegistry.TryGetValue(matcher, out matcherType))
            {
                return (IPatternMatcher)Activator.CreateInstance(matcherType);
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


