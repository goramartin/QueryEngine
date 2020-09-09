
/*! \file
  
This class includes definitions of dfs search algorithms used to find pattern defined in query match expression.
  
Algorithm is given a pattern to find and the algorithm iterates over elements of the graph and 
communicates with given pattern.
Chains of the pattern can form a connected chains (that is that some chains can be directly connected via
certain vairables). The connected chains from a conjunction and the search algorithm uses the connection to
avoid unneccessary iteration over graph elements. 
More about the algorithm can be found at the method definitions.
  
So far, there have been two types of dfs matches. One single threaded and one parallel. 
The one single threaded should not be used alone because it was made to be used by the parallel.
The parallel algorithm is lock-free algorithm, saving results have been made lock free thanks to 
storing result into their own place inside query result structure (thread index).
And division of work is done lock free thanks to interlocked class that allows to perform 
certain operation atomicaly.
  
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.CodeDom;
using System.IO;

namespace QueryEngine
{

    /// <summary>
    /// This instance should be created only inside parallel dfs searcher
    /// Class represents DFS search that accepts patterns with IDFSPattern interface.
    /// The search is separated into conjuctions from pattern. That is to say, each conjuction has got
    /// one starting pattern chain, from that chain we connect subsequent chains based on matched variables from this chain.
    /// Simply each conjunction represents chains that can be connected by certain variables.
    /// 
    /// Algorithm tries to start search from every vertex on each conjunction.
    /// Bear in mind, that variables of each separate conjunction disregarding its connection to the others is 
    /// dependent on the matched variables from the other conjunctions.
    /// </summary>
    internal sealed class DFSPatternMatcher : ISingleThreadMatcher
    {
        private readonly Graph graph;
        private readonly DFSPattern pattern;
        private readonly Element[] matchedElements;
        private readonly List<Element>[] results;
        private bool processingVertex;
        private bool isStoringResults;
        private int startVerticesIndex;
        private int startVerticesEndIndex;
        private int NumberOfMatchedElements;

        /// <summary>
        /// Starting vertices are implicitly set to entire graph.
        /// </summary>
        /// <param name="pat"> The pattern to find.</param>
        /// <param name="gr"> The graph to search. </param>
        /// <param name="res"> The object to store found results. </param>
        public DFSPatternMatcher(IDFSPattern pat, Graph gr, List<Element>[] res)
        {
            if (gr == null || pat == null || res == null)
                throw new ArgumentException($"{this.GetType()}, passed null to a constructor.");

            this.graph = gr;
            this.matchedElements = new Element[pat.AllNodeCount];
            this.pattern = (DFSPattern)pat;
            this.results = res;
            
            // Implicit range of vertices to iterate over the entire graph.
            this.startVerticesIndex = 0;
            this.startVerticesEndIndex = gr.vertices.Count;
        }

        /// <summary>
        /// Method initiates search from every conjunction.
        /// Once one conjunction is finished filling its chains, it returns an index of last used vertex.
        /// That index is then stored and once we jump to subsequent conjunction and return back to the same conjunction later on,
        /// we use this stored index to continue with the vertex on that index in the search.
        /// </summary>
        public void Search()
        {
            int[] lastUsedIndeces = new int[pattern.PatternCount];
            lastUsedIndeces.Populate(-1);

            int lastUsedIndex = -1;

            while (true)
            {
                // -1 meaning that next conjunction search will start from the beginning of vertex list. 
                if (lastUsedIndex == -1) lastUsedIndex = DFSStartOfCunjunction(0, false);
                // Else it uses last used index in that conjunction.
                else lastUsedIndex = DFSStartOfCunjunction(lastUsedIndex, true);

                // - 1 one finished whole pattern -> need to prepare previous pattern or we have finished searching
                if (lastUsedIndex == -1)
                {
                    if (pattern.CurrentPatternIndex == 0) break; // end of search
                    pattern.PreparePreviousSubPattern();
                    lastUsedIndex = lastUsedIndeces[pattern.CurrentPatternIndex];
                }
                else // Pick next pattern and save last used vertex
                {
                    lastUsedIndeces[pattern.CurrentPatternIndex] = lastUsedIndex;
                    pattern.PrepareNextSubPattern();
                    lastUsedIndex = -1;
                }
            }
        }

        /// <summary>
        /// Initiates iteration over one connected pattern (patterns that share a variable).
        /// We try to start search from each vertex in the graph.
        /// Algorithm is divided into filling up chains (patterns).
        /// Once one chain is filled/depleted we return from main search loop.
        /// Filled: We check whether next pattern is part of the conjuntion (shared variable is on its start) or there is another conjunction.
        /// If there is another conjunction we return the index of the last used vertex that will be later used to init currently running conjunction search.
        /// Otherwise we prepare next sub pattern chain and pick an element that connects that pattern, it will be immediately matched.
        /// Depleted: It is checked if current pattern chain is a starting point of a conjunction, if it is, we either pick next vertex from the graph
        /// or we just return -1, that is we finished searching this conjunction.
        /// If it is not the starting point, we simply prepare previous sub pattern and set next element to null,
        /// which will lead to dfs backwards once the main loop is started.
        /// </summary>
        /// <param name="lastIndex"> Last index from last iteration. </param>
        /// <param name="cameFromUp"> If we came from a different conjunction. </param>
        /// <returns> Last used index. </returns>
        public int DFSStartOfCunjunction(int lastIndex, bool cameFromUp)
        {
            var vertices = this.graph.vertices;
            //for (int i = lastIndex; i < vertices.Count; i++)
            for (int i = this.PickConjunctionStartIndex(lastIndex, cameFromUp); i < this.PickConjunctionEndIndex(); i++)
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
                    // Note: 
                    // true is only when current pattern is filled and there are more patterns
                    // false is only when current pattern is emptied and need to return to a previous pattern or take another starting vertex
                    var canContinue = DFSMainLoop(nextElement);

                    // If there are more chains
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
                    }
                    else
                    {
                        // If we are at the starting chain of a conjunction we let main for loop pick next starting vertex.
                        if ((pattern.GetCurrentChainConnection()) == null) break;
                        // If we are connected to the before pattern through a variable, we will initiate returning.
                        else
                        {
                            pattern.PreparePreviousSubPattern();
                            nextElement = null;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Main loop of the dfs, always consumes one sub pattern from patterns.
        /// When it returns we expect the sub pattern to be empty of full.
        /// 
        /// The pattern is full only when there are more patterns to search.
        /// Otherwise it is empty.
        /// </summary>
        /// <param name="nextElement"> Element to start on. </param>
        /// <returns> True if there is another pattern (Filled), False if it is returning.(Empty) </returns>
        private bool DFSMainLoop(Element nextElement)
        {
            while (true)
            {
                // Try to apply the new element to the pattern.
                bool success =  ((nextElement != null) && pattern.Apply(nextElement));
                if (success)
                {
                    // If it is the last node in the pattern, we check if it is the last pattern.
                    AddToResult(nextElement);
                    if (pattern.isLastNodeInCurrentPattern())
                    {
                        if (pattern.isLastPattern())
                        {
                            // Setting null here makes it to fail on next iteration and it is forced to dfs back.
                            StoreResult();
                            nextElement = null;
                            continue;
                        }
                        // There is more patterns to fill.
                        else return true;
                    }
                    pattern.PrepareNextNode();
                    nextElement = DoDFSForward(nextElement, null);
                }
                else
                {
                    nextElement = DoDFSBack(nextElement);

                    // Check if we came back from top, meaning we finished all possible matches in this chain.
                    if (pattern.CurrentMatchNodeIndex <= 0)
                    {
                        // The 0th variable must be unset here, because we cant anticipate if we failed on 0th match.
                        ClearCurrentFromResult();
                        pattern.PreparePreviousNode();
                        break;
                    }
                }
            }
            return false;
        }




        /// <summary>
        /// Method searches for the next element to match.
        /// 
        /// Processing Vertex:
        /// If the last matched element is vertex we look for an edge.
        /// 
        /// Processing Edge:
        /// If the last matched element is edge we just take the end vertex of the edge.
        /// 
        /// Notes:
        /// Last used edge is not null only when calling from dfs back method.
        /// In this case we do not add anything to the result.
        /// </summary>
        /// <param name="lastUsedElement"> Last matched Element. </param>
        /// <param name="lastUsedEdge"> Last matched edge. </param>
        /// <returns> Next element that will be tried to be applied. </returns>
        private Element DoDFSForward(Element lastUsedElement, Edge lastUsedEdge)
        {
            if (processingVertex)
            {
                Edge nextEdge = FindNextEdge(pattern.GetMatchType(), (Vertex)lastUsedElement, lastUsedEdge);
                processingVertex = false;
                return nextEdge;
            }
            else
            {
                processingVertex = true;
                return ((Edge)lastUsedElement).EndVertex;
            }
        }


        /// <summary>
        /// Processing Vertex:
        /// We are returning from the dfs.
        /// When processing the vertex, we failed to add the vertex to the pattern, 
        /// that means we need to go down in the pattern and also remove the edge we came from to the vertex. 
        /// In order to do so, we will return null, next loop in algorithm behaves as if it fails on adding an edge,
        /// so the edge gets removed.
        /// 
        /// Processing Edge:
        /// When processing edge, we get the last used edge from the result and remove it from result array.
        /// (Note there can be no edge, eg: we failed to add one at all.)
        /// We take the edge (null or normal edge) and try to do dfs from the vertex the edge started from 
        /// If it returns a new edge we can continue trying to apply the edge on the same index in pattern.
        /// If it is null we need to remove also the vertex because there are no more available edges from this vertex.
        /// In order to do that we go down in pattern that is done via returning null, so the algorithm behaves as if it failed 
        /// on adding a vertex. (Note that after fail, it jumps here again and so on.)
        /// </summary>
        /// <param name="lastElement"> Last element we failed on. Used only if it was an edge. </param>
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
                // Take the edge on the current position in result array. (Edge that was matched before, can be null if no edge was there.)
                Element lastUsedEdgeInResult = (Edge)matchedElements[pattern.OverAllIndex];

                // lastElement is null only when we are returning from the removed vertex -> we take the last used edge in the result.
                // Else we always use the newest edge we failed on. 
                if (lastElement == null) lastElement = lastUsedEdgeInResult;

                // Clears the last used edge from result, or does nothing (no edge was there).
                // It needs to clean the scope so that it can apply next possible edge if one is found.
                ClearCurrentFromResult();
                pattern.UnsetCurrentVariable();


                // Try to find new edge from the last vertex.
                processingVertex = true; // To jump into dfs forward.
                Element nextElement =
                    DoDFSForward((Vertex)matchedElements[pattern.OverAllIndex - 1], (Edge)lastElement);

                // If no edge was found, we want to remove also the last vertex. (because we consumed all of his edges)
                // Returning null in this position removes the vertex in the next cycle of the main algorithm.
                // Else we continue in searching with the new edge on the same index of the match node.
                if (nextElement == null)
                {
                    // Before we move back, it must unsets last edge type of current match node.
                    pattern.PreparePreviousNode();
                    processingVertex = true;
                    return null;
                }
                else return nextElement;
            }
        }

        /// <summary>
        /// Finds next edge based on given type.
        /// </summary>
        /// <param name="edgeType"> Type of edge. </param>
        /// <param name="lastUsedVertex"> Vertex that the edge is coming from. </param>
        /// <param name="lastUsedEdge"> Possibly, last used edge of the vertex. </param>
        /// <returns> Next edge. </returns>
        private Edge FindNextEdge(Type edgeType, Vertex lastUsedVertex, Edge lastUsedEdge)
        {
            if (edgeType == typeof(InEdge)) return FindInEdge(lastUsedVertex, lastUsedEdge);
            else if (edgeType == typeof(OutEdge)) return FindOutEdge(lastUsedVertex, lastUsedEdge);
            else if (edgeType == typeof(Edge)) return FindAnyEdge(lastUsedVertex, lastUsedEdge);
            else throw new ArgumentException($"{this.GetType()}, matcher got invalid type of edge. Type = {edgeType}.");
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
        /// Fixed errors when returning from future pattern which caused using different edge types and infinite loop.
        /// Notice that this method is called only when matching type Any of the edge.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched for next possible. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next edge of the vertex. </returns>
        private Edge FindAnyEdge(Vertex vertex, Edge lastUsedEdge)
        {
            Edge nextEdge = null;
            // If no edge has been used -> pick in edge /or/ it hasnt finished iteration over in edges 
            if (lastUsedEdge == null || lastUsedEdge.GetElementType() == typeof(InEdge))
            {
                nextEdge = FindInEdge(vertex, lastUsedEdge);
                if (nextEdge == null) lastUsedEdge = null;
                else return nextEdge;
            }
            // if all in edges were used -> pick out edges
            nextEdge = FindOutEdge(vertex, lastUsedEdge);
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
        private Edge GetNextEdge<T>(int start, int end, List<T> edges, Edge lastUsedEdge) where T : Edge
        {
            // The processed vertex have not got edges.
            if (start == -1) return null;
            // No edge was used from the processed vertex -> pick the first one.
            else if (lastUsedEdge == null) return edges[start];
            // The Last processed Edge was the last edge of the vertex -> can not pick more edges.
            else if (end - 1 == lastUsedEdge.PositionInList) return null;
            // There are more non processed edges of the vertex -> pick the following one from the edge list.
            else return edges[lastUsedEdge.PositionInList + 1];
        }

        /// <summary>
        /// Adds element to the result.
        /// </summary>
        /// <param name="element">Element to be added to result.</param>
        private void AddToResult(Element element)
        {
            matchedElements[pattern.OverAllIndex] = element;
        }

        /// <summary>
        /// Removes the last element from the result.
        /// </summary>
        private void ClearCurrentFromResult()
        {
            matchedElements[pattern.OverAllIndex] = null;
        }


        /// <summary>
        /// Based on which conjunction we are filling we set set its starting vertices.
        /// If it is the first conjunction in the pattern we iterate over vertices received from vertex distributor.
        /// </summary>
        /// <returns> Starting index of the conjunction. </returns>
        private int PickConjunctionStartIndex(int lastIndex, bool cameFromUp)
        {
            if (this.pattern.CurrentPatternIndex == 0 &&
                (this.pattern.CurrentMatchNodeIndex == 0 || this.pattern.isLastNodeInCurrentPattern()))
                return cameFromUp ? lastIndex : this.startVerticesIndex;
            else return lastIndex;
        }

        /// <summary>
        /// Based on which conjunction we are filling we set set its starting vertices.
        /// If it is the first conjunction in the pattern we iterate over vertices received from vertex distributor.
        /// </summary>
        /// <returns> Ending index of the conjunction. </returns>
        private int PickConjunctionEndIndex()
        {
            if (this.pattern.CurrentPatternIndex == 0 &&
                (this.pattern.CurrentMatchNodeIndex == 0 || this.pattern.isLastNodeInCurrentPattern()))
                return this.startVerticesEndIndex;
            else return this.graph.vertices.Count;
        }

        /// <summary>
        /// Stores matched variables into result class based on columns and thread index.
        /// </summary>
        private void StoreResult()
        {
            var scope = this.pattern.GetMatchedVariables();
            this.NumberOfMatchedElements++;

            if (this.isStoringResults)
            {
                for (int i = 0; i < this.results.Length; i++)
                    this.results[i].Add(scope[i]);
            }
        }

        /// <summary>
        /// Method sets range of vertices for the first conjunction in the pattern.
        /// </summary>
        /// <param name="start"> Starting index.</param>
        /// <param name="end"> Ending index. </param>
        public void SetStartingVerticesIndeces(int start, int end)
        {
            if (start < 0 || end < 0)
                throw new ArgumentException($"{this.GetType()}, starting vertices are empty on thread {Thread.CurrentThread.ManagedThreadId} .");
            else
            {
                this.startVerticesIndex = start;
                this.startVerticesEndIndex = end;
            }
        }

        /// <summary>
        /// Sets value whether the matcher should store its results or not.
        /// </summary>
        /// <param name="storeResults"> True if the matcher must store results, otherwise false. </param>
        public void SetStoringResults(bool storeResults)
        {
            this.isStoringResults = storeResults;
        }

        public int GetNumberOfMatchedElements() => this.NumberOfMatchedElements;

    }
}
