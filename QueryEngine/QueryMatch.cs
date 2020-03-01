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







    //Class representing single step of pattern to match.
    //Method apply returns true if the element can be added to final result.
    abstract class DFSBaseMatch
    {
        public enum MatchType { Vertex, Edge};
        protected bool anonnymous;
        protected bool repeatedVariable;
        protected int positionOfRepeatedField;
        protected Table type;

        public abstract bool Apply(Element element, Element[] scope);

        protected bool CheckCommonConditions(Element element, Element[] scope)
        {
            // Check type, comparing references to tables.
            if ((this.type != null) && (this.type != element.GetTable())) return false;

            // It is anonnymous, then it can match any vertex/edge.
            if (this.anonnymous) return true;

            // It is repetition of variable before
            if (this.repeatedVariable)
            {
                // The variable was used, check if it has got the same id.
                if (!this.IsFirstAppereance(scope))
                {
                    if (scope[positionOfRepeatedField].GetID() != element.GetID()) return false;
                    else { /* empty else no other option*/ }

                } else // Else check if the element can be used at the variable ( check other variables for similarity )
                {
                    for (int i = 0; i < scope.Length; i++)
                    {
                        Element tmpEl = scope[i];

                        // The element occupies different variable
                        if (this.positionOfRepeatedField != i && tmpEl.GetID() == element.GetID()) return false;

                        // if the variable is occupied in its slot
                        if (this.positionOfRepeatedField == i && tmpEl != null) return false;


                    }
                    
                    // The element can be used, place him into the scope and return true;
                     scope[this.positionOfRepeatedField] = element;
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
        /// </summary>
        /// <param name="elements"></param>
        /// <returns> True on empty </returns>
        public bool IsFirstAppereance(Element[] elements)
        {
            if (elements[this.positionOfRepeatedField] == null) return true;
            else return false;
        }

        public void SetType(Table t) => this.type = t;
        public Table GetTable() => this.type;
        

    }
    class DFSVertexMatch : DFSBaseMatch
    {
        public DFSVertexMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        public DFSVertexMatch(ParsedPatternNode node, int indexInMap)
        {
            if (indexInMap != -1)
            {
                this.repeatedVariable = true;
                this.anonnymous = false;
            }
            this.positionOfRepeatedField = indexInMap;
            this.type = node.table;
        }

        public override bool Apply(Element element,Element[] scope)
        {
            if (element == null) return false;
            else if (!(element is Vertex)) return false;
            else return CheckCommonConditions(element, scope);
        }


        public override bool Equals(object obj)
        {
            if (obj is DFSVertexMatch)
            {
                var o = obj as DFSVertexMatch;
                if (this.GetTable() != o.GetTable()) return false;
                return true;
            }
            return false;

        }

    }
    class DFSEdgeMatch : DFSBaseMatch
    {
        protected EdgeType edgeType;

        public DFSEdgeMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        public DFSEdgeMatch(ParsedPatternNode node, int indexInMap)
        {
            if (indexInMap != -1) { 
                this.repeatedVariable = true; 
                this.anonnymous = false; 
            }
            this.positionOfRepeatedField = indexInMap;
            this.type = node.table;
            this.edgeType = node.edgeType;
        }

        public override bool Apply(Element element, Element[] scope)
        {
            if (element == null) return false;
            else if (!(element is Edge)) return false;
            else return CheckCommonConditions(element, scope);
        }

        public EdgeType GetEdgeType() => this.edgeType;
        public void SetEdgeType(EdgeType type) => this.edgeType = type;

        public override bool Equals(object obj)
        {
            if (obj is DFSEdgeMatch)
            {
                var o = obj as DFSEdgeMatch;
                if (o.edgeType != this.edgeType) return false;
                if (this.GetTable() != o.GetTable()) return false;
                return true;
            }
            return false;

        }

    }





    interface IPattern
    {
        /*
        bool Apply(Element element);
        int IndexOfCurrentPattern();
        int indexOfCurrentMatchNode();

        int PatternCount();
        int NodeCountInCurrentPattern();
         */
    }
    
    class DFSPattern : IPattern
    {
        private List<List<DFSBaseMatch>> Patterns;
        private int CurrentPattern;
        private int CurrentMatchNode;
        private Element[] Scope;

        public DFSPattern(VariableMap map, List<ParsedPattern> parsedPatterns)
        {
            this.Patterns = new List<List<DFSBaseMatch>>();
            this.CreatePattern(parsedPatterns, map);
            this.Scope = new Element[map.GetCount()];
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
        private void CreatePattern(List<ParsedPattern> parsedPatterns, VariableMap variableMap)
        {
            var orderedPatterns = OrderParsedPatterns(parsedPatterns);

            // For every Parsed Pattern
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                // Try to split it.
                var firstPart = parsedPatterns[i].SplitParsedPattern();       
                
                // If the parsed pattern was splited
                // Add both parts into the real Pattern
                if ( firstPart != null)
                {
                    this.Patterns.Add(CreateChain(firstPart.Pattern, variableMap));
                }
                this.Patterns.Add(CreateChain(parsedPatterns[i].Pattern, variableMap));

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
                // Take subsequent patterns
                for (int j = i + 1; j < parsedPatterns.Count; j++)
                {
                    var otherParsedPattern = parsedPatterns[j];
                    if (currentParsedPattern.TryFindEqualVariables(otherParsedPattern, out string varName))
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
        /// <param name="p"> Parsed pattern </param>
        /// <param name="map"> Map to store info about veriables </param>
        /// <returns></returns>
        private List<DFSBaseMatch> CreateChain(List<ParsedPatternNode> p, VariableMap map) 
        {
            List<DFSBaseMatch> tmpChain = new List<DFSBaseMatch>();

            // For each parsed pattern node
            for (int i = 0; i < p.Count; i++)
            {
                var tmpNode = p[i];
                int index = -1;
                
                // If it has not got a name, do not add it to map.
                if (tmpNode.name != null)
                {
                    // Try if the variable is inside a dictionary
                    if ( (index = map.GetVariablePosition(tmpNode.name)) == -1)
                    {
                        // If it is not, Add it there with the proper type and index.
                        index = map.GetCount();
                        map.AddVariable(tmpNode.name,index, tmpNode.table);
                    }
                }

                // Create match node and add it to the chain.
                if (tmpNode.isVertex)
                      tmpChain.Add(CreateDFSBaseMatch(DFSBaseMatch.MatchType.Vertex, p[i], index));
                else tmpChain.Add(CreateDFSBaseMatch(DFSBaseMatch.MatchType.Edge, p[i], index));
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
        private DFSBaseMatch CreateDFSBaseMatch(DFSBaseMatch.MatchType type, ParsedPatternNode node, int indexInMap)
        {
            switch (type)
            {
                case DFSBaseMatch.MatchType.Vertex:
                    return new DFSVertexMatch(node, indexInMap);
                case DFSBaseMatch.MatchType.Edge:
                    return new DFSEdgeMatch(node, indexInMap);
                default:
                    throw new ArgumentException($"{this.GetType()} Trying to create Match type that does not exit.");
            }
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
                int p = ((Vertex)lastElement).GetPositionInVertices();
                if (edgeType == EdgeType.InEdge) nextElement = ProcessInEdge(p, lastUsedEdge);
                else if (edgeType == EdgeType.OutEdge) nextElement = ProcessOutEdge(p, lastUsedEdge);
                else nextElement = ProcessAnyEdge(p, lastUsedEdge);

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

        private Element FindNextEdge(int start, int end, List<Edge> edges, Element lastUsedEdge)
        {
            if (start == -1) return null;
            else if (lastUsedEdge == null) return edges[start];
            else if (end - 1 == lastUsedEdge.positionInList) return null;
            else return edges[lastUsedEdge.positionInList + 1];
        }
        private Element ProcessInEdge(int p, Element last)
        {
            graph.GetRangeToLastEdgeOfVertex(isOut: false, p, out int start, out int end);
            return FindNextEdge(start, end, graph.GetAllInEdges(), last);
        }
        private Element ProcessOutEdge(int p, Element last)
        {
            graph.GetRangeToLastEdgeOfVertex(isOut: true, p, out int start, out int end);
            return FindNextEdge(start, end, graph.GetAllOutEdges(), last);
        }
        private Element ProcessAnyEdge(int p, Element last)
        {
            Element e = null;
            if (processingInEdge)
            {
                e = ProcessInEdge(p, last);
                if (e == null) { last = null; processingInEdge = false; }
            }
            if (!processingInEdge) e = ProcessOutEdge(p, last);
            return e;
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


