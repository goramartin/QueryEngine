using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    //Query represents query information, carrying it in each of the query main words.
    class Query
    {
        Scope scope;
        SelectObject select;
        MatchObject match;

        //needs to be rearanged
        public Query(SelectObject s, MatchObject m, Scope scope)
        {
            this.select = s;
            this.match = m;
            this.scope = scope;
        }

        public List<List<BaseMatch>> GetMatchPattern() { return this.match.GetPattern(); }

    }

    
    
    /// <summary>
    /// Scope represents scope of variables in the whole query during pattern matching.
    /// When the Pattern is flattned the integer value that resides on given variable name
    /// corresponds to the index in the flattened pattern.
    /// That is done because we need to retrieve the variable from the matched elements from within the flattened pattern.
    /// 
    /// Each variable is inserted only once despite possible multiple occurences of the same variable.
    /// The main purpose of the scope is to obtain Elements that is the repetition does not change the desired value.
    /// </summary>
    class Scope
    {
        private Dictionary<string, int > scopeVar;

        public Scope(Dictionary<string,int> sv)=> this.scopeVar = sv;
        public Scope() => this.scopeVar = new Dictionary<string, int>();

        public Dictionary<string, int> GetScopeVariables() => this.scopeVar;

        /// <summary>
        /// Adds variable to the dictionary.
        /// </summary>
        /// <param name="varName"> Name of variable to insert </param>
        /// <param name="position"> Position in flattened pattern </param>
        public void AddVariable( string varName, int position) 
        {
            if (this.scopeVar.ContainsKey(varName)) throw new ArgumentException($"{this.GetType()} Variable is already in the Score.");
            else this.scopeVar.Add(varName, position);
            
        }

        /// <summary>
        /// Returns positon of variable based on the name of the variable.
        /// </summary>
        /// <param name="name"> Variable to be searched for in Dictionary. </param>
        /// <returns> Position of variable in flattened pattern </returns>
        public int GetVariablePosition(string name)
        {
            if (this.scopeVar.TryGetValue(name, out int position)) return position;
            else return -1;
        }

    }


    /// <summary>
    /// Select represents list of variables to be printed.
    /// List of select variables contains names and proprty names to be printed from the result.
    /// </summary>
    class SelectObject
   {
        private List<SelectVariable> selectVariables;
        public SelectObject(List<SelectVariable> sv) => this.selectVariables = sv;
        public List<SelectVariable> GetSelectVariables() => this.selectVariables;

        // to do
        public void CheckCorrectnessOfSelect(Dictionary<string, Table> nodeTables, Dictionary<string, Table> edgeTables, Scope scope)
        {






        }
   }
    class SelectVariable
    {
        public string name { get; private set; } 
        public string propName { get; private set; }

        public bool TrySetName(string n)
        {
            if (this.name == null) { this.name = n; return true; }
            else return false;
        }
        public bool TrySetPropName(string n)
        {
            if (this.propName == null) { this.propName = n; return true; }
            else return false;
        }
        public bool IsEmpty()
        {
            if ((this.name == null) && (this.propName == null)) return true;
            else return false;
        }
    }






    /// <summary>
    /// Match represents pattern to match in main match algorithm also it checks th correctness of the pattern when creating it.
    /// The pattern is created from List of Parsed Patterns passed from Visitor that processes Match expression.
    /// </summary>
    class MatchObject
    {
        private List<List<BaseMatch>> pattern;

        public MatchObject() { this.pattern = new List<List<BaseMatch>>(); }



        /// <summary>
        /// Creates pattern from Parsed Pattern made by match visitor, also creates scope for variables
        /// during pattern matching.
        /// Given pattern is check for correctness and ordered so each connected patterns go after each 
        /// Then the resulting pattern is created. Patterns to be splited are splited into two based on split variable.
        /// For example: (a) -> (b) -> (c) splited by var. b == (b) <- (a) , (b) -> (c)
        /// </summary>
        /// <param name="parsedPatterns"> Pattern created by Match Visitor </param>
        /// <param name="scope"> Query scope of variable (empty) </param>
        public void CreatePattern(List<ParsedPattern> parsedPatterns, Scope scope)
        {
            CheckCorrectness(parsedPatterns);
            var orderedPatterns = OrderPatterns(parsedPatterns);







            Console.ReadLine();




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
        private List<ParsedPattern> OrderPatterns(List<ParsedPattern> parsedPatterns)
        {
            List<ParsedPattern> result = new List<ParsedPattern>();
            bool[] usedPatterns = new bool[parsedPatterns.Count];
            usedPatterns.Populate(false);
            
            // For each pattern in ParsedPatterns
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                var currentParsedPattern = parsedPatterns[i];
                // Take subsequent patterns
                for (int j = i+1; j < parsedPatterns.Count; j++)
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
        /// Throws error when the given pattern is fault.
        /// Fault pattern contains one of: No variables, Discrepant variable definitions
        /// discrepant type definitions
        /// Correctness is checked only against the first appearance of the variable.
        /// </summary>
        /// <param name="parsedPatterns"></param>
        private void CheckCorrectness(List<ParsedPattern> parsedPatterns)
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

        public List<List<BaseMatch>> GetPattern() => this.pattern;
    }


    interface IPatternMatcher
    {
        void Search();
    }

    class DFSPatternMatcher : IPatternMatcher
    {
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
            int[] lastUsedVertices = new int[pattern.Count-1];
            int lastUsedVertex = -1;
            while (true){
                if (lastUsedVertex == -1) lastUsedVertex = DFS(0, false);
                else lastUsedVertex = DFS(lastUsedVertex,true);
                if (lastUsedVertex == -1) {
                    if (patternIndex == 0) break;
                    patternIndex--;
                    lastUsedVertex = lastUsedVertices[patternIndex];
                    currentPattern = pattern[patternIndex];
                }
                else{
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
            for (int i = position; i < vertices.Count; i++) {
                processingVertex = true;
                processingInEdge = true;
                Element nextElement = vertices[i];
                currentPatternIndex = 0;
                if (cameFromUp)
                {
                    nextElement = null;
                    currentPatternIndex = currentPattern.Count-1;
                    cameFromUp = false;
                }
                while (true)
                {
                    bool success = currentPattern[currentPatternIndex].Apply(nextElement, pattern, result);
                    if (success) {
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
                        if (currentPatternIndex <= 0) break ; 
                    }
                }
            }
            return -1;
        }

        private Element DoDFSForward(Element lastElement, Edge lastUsedEdge)
        {
            if (processingVertex){
                EdgeType edgeType = ((EdgeMatch)currentPattern[currentPatternIndex]).GetEdgeType();

                Element nextElement = null;
                int p = ((Vertex)lastElement).GetPositionInVertices();
                if (edgeType == EdgeType.InEdge) nextElement = ProcessInEdge(p, lastUsedEdge);
                else if (edgeType == EdgeType.OutEdge) nextElement = ProcessOutEdge(p, lastUsedEdge);
                else nextElement = ProcessAnyEdge(p, lastUsedEdge);

                processingVertex = false;
                return nextElement;
            }
            else {
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
            if (processingVertex) {
                RemoveFromResult(currentPatternIndex);
                currentPatternIndex--;
                processingVertex = false;
                return null;
            }
            else {
                Edge lastUsedEdge = (Edge)result[GetAbsolutePosition(currentPatternIndex)];
                if (element == null) element = lastUsedEdge;
                RemoveFromResult(currentPatternIndex);

                processingVertex = true; //To jump into dfs.
                Element nextElement = 
                    DoDFSForward(result[GetAbsolutePosition(currentPatternIndex) - 1], (Edge)element);
                if (nextElement == null) {
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
            graph.GetRangeToLastEdgeOfVertex(isOut:false, p, out int start, out int end);
            return FindNextEdge(start, end, graph.GetAllInEdges(), last);
        }
        private Element ProcessOutEdge(int p, Element last)
        {
            graph.GetRangeToLastEdgeOfVertex(isOut:true, p, out int start, out int end);
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
    }





    //Class representing single step of pattern to match.
    //Method apply returns true if the element can be added to final result.
    abstract class BaseMatch
    {
        protected bool anonnymous;
        protected bool repeatedVariable;
        protected int positionOfRepeatedField;
        protected Table type;

        public abstract bool Apply(Element element, List<List<BaseMatch>> baseMatches, Element[] result);

        protected bool CheckCommonConditions(Element element, List<List<BaseMatch>> baseMatches, Element[] result)
        {
            //Check type, comparing references to tables.
            if ((this.type != null) && (this.type != element.GetTable())) return false;
            
            //It is anonnymous, then it can match any vertex/edge.
            if (this.anonnymous) return true;

            //It is repetition of variable before, check if it has same id.
            if (repeatedVariable)
                if (result[positionOfRepeatedField].GetID() != element.GetID()) return false;

            //Check if the element is not set for another variable.
            for (int i = 0; i < result.Length; i++) {
                Element tmpEl = result[i];
                
                //Further ahead, there are no elements stored in result.
                if (tmpEl == null) break;
                
                if (tmpEl.GetID() == element.GetID()) {
                    BaseMatch tmp = baseMatches.GetMatch(i);
                    if (tmp.IsAnonnymous()) continue;
                    else if (i == this.positionOfRepeatedField) continue;
                    else if ((tmp.positionOfRepeatedField != -1) &&
                            (tmp.positionOfRepeatedField == this.positionOfRepeatedField)) continue;
                    else return false;
                }
            }

            return true;
        }

        public void SetAnnonymous(bool b) => this.anonnymous = b;
        public void SetRepeated(bool b) => this.repeatedVariable = b;
        public void SetPositionOfRepeatedField(int p) => this.positionOfRepeatedField = p;
        public void SetType(Table t) => this.type = t;
        public Table GetTable() => this.type;
        public bool IsRepeated() => this.repeatedVariable;
        public int GetPositionOfRepeatedField() => this.positionOfRepeatedField;
        public bool IsAnonnymous() => this.anonnymous;

    }
    class VertexMatch : BaseMatch
    {
        public VertexMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        public override bool Apply(Element element, List<List<BaseMatch>> baseMatches, Element[] result)
        {
            if (element == null) return false;
            else if (!(element is Vertex)) return false;
            else return CheckCommonConditions(element, baseMatches, result);
        }


        public override bool Equals(object obj)
        {
            if (obj is VertexMatch)
            {
                var o = obj as VertexMatch;
                if (this.GetTable() != o.GetTable()) return false;
                return true;
            }
            return false;

        }

    }
    class EdgeMatch : BaseMatch
    {
        protected EdgeType edgeType;

        public EdgeMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        public override bool Apply(Element element, List<List<BaseMatch>> baseMatches, Element[] result)
        {
            if (element == null) return false;
            else if (!(element is Edge)) return false;
            else return CheckCommonConditions(element, baseMatches, result);
        }

        public EdgeType GetEdgeType() => this.edgeType;
        public void SetEdgeType(EdgeType type) => this.edgeType = type;

        public override bool Equals(object obj)
        {
            if (obj is EdgeMatch)
            {
                var o = obj as EdgeMatch;
                if (o.edgeType != this.edgeType) return false;
                if (this.GetTable() != o.GetTable()) return false;
                return true;
            }
            return false;

        }

    }

}
