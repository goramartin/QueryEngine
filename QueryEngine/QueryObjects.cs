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

        public Query(SelectObject s, MatchObject m, Scope scope)
        {
            this.select = s;
            this.match = m;
            this.scope = scope;
        }

        public List<List<BaseMatch>> GetMatchPattern() { return this.match.GetPattern(); }




        /// <summary>
        /// ////////// NOW WILL BE DONE IN SELECT OBJECT
        /// </summary>
        /// <returns></returns>
        //Check if variables in select correspond to variables in scope
        public bool CheckCorrectnessOfQuery()
        {
            var sc = scope.GetScopeVariables();
            if (sc.Count == 0) return false;
            var pattern = match.GetPattern();
            foreach (var item in select.GetSelectVariables())
            {
                if (item.name == "*") continue;
                if (!sc.TryGetValue(item.name, out int p)) return false;
                
                //If select needs property, we check if the property is correct.
                if (item.propName != null) 
                {
                    if (pattern.GetMatch(p).GetTable() == null) return false;
                    if (!pattern.GetMatch(p).GetTable().ContainsProperty(item.propName)) return false;    
                }
            }
            return CheckCorrectnessOfPatternTypes(pattern);
        } 

        
        
        /// <summary>
        /// ///////// NOW WILL BE DONE IN MATCHOBJECT
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        //Check whether in the pattern the repeating variables are same.
        private bool CheckCorrectnessOfPatternTypes(List<List<BaseMatch>> pattern)
        {
            for (int i = 0; i < pattern.Count; i++)
            {
                for (int k = 0; k < pattern[i].Count; k++)
                {
                    BaseMatch b = pattern[i][k];
                    if (b.IsRepeated())
                    if (!b.Equals(
                            pattern.GetMatch(b.GetPositionOfRepeatedField()))) 
                            return false;
                }
            }
            return true;
        }
        
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
        public void CheckCorrectnessOfSelect()
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


        //to do

        /// <summary>
        /// Creates pattern from Parsed Pattern made by match visitor, also creates scope for variables
        /// during pattern matching.
        /// </summary>
        /// <param name="parsedPatterns"></param>
        /// <param name="scope"></param>
        public void CreatePattern(List<ParsedPattern> parsedPatterns, Scope scope)
        {
            CheckCorrectness(parsedPatterns);
            FillConnections(parsedPatterns);







        }


        // to do

        /// <summary>
        /// Fills connection dictionaries that represent connection between other patterns and on which variable they connects.
        
        /// </summary>
        /// <param name="parsedPatterns"> Parser Pattern from MatchVisitor</param>
        private void FillConnections(List<ParsedPattern> parsedPatterns)
        {
            for (int i = 0; i < parsedPatterns.Count; i++)
            {
                for (int j = i+1; j < parsedPatterns.Count; j++)
                {






                }
            }
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
