using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    interface IPatternMatcher
    {
        void Search();
    }

    interface ISingleThreadMatcher : IPatternMatcher
    {
        void SetStartingVertices(List<Vertex> vertices);
    }

    interface IParallelMatcher : IPatternMatcher
    {

    }


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
    class DFSPatternMatcher : ISingleThreadMatcher
    {
        private Graph graph;
        private IDFSPattern pattern;
        private Element[] result;
        private bool processingVertex;
        private int resultIndex; // Based on thread, implicitly 0
        private List<Vertex> startingVertices;
        private QueryResults results;

        /// <summary>
        /// Starting vertices are implicitly set to entire graph.
        /// </summary>
        /// <param name="pattern"> Pattern to find.</param>
        /// <param name="graph"> Graph to search. </param>
        /// <param name="results"> Object to store found results. </param>
        /// <param name="resultIndex"> Index to store results on. =>0 </param>
        public DFSPatternMatcher(IDFSPattern pattern, Graph graph, QueryResults results, int resultIndex)
        {
            if (pattern == null || graph == null || results == null) 
                throw new ArgumentException($"{this.GetType()}, passed null to a constructor.");

            this.graph = graph;
            this.result = new Element[pattern.AllNodeCount];
            this.pattern = pattern;
            this.results = results;
            this.resultIndex = resultIndex;
            this.startingVertices = graph.vertices;
        }

        /// <summary>
        /// Method initiates search from every conjunction.
        /// Once one conjunction is finished filling its chains, it returns  an index of last used vertex.
        /// That index is then stored and once we jump to another junctions and return back later on, we use this index
        /// to continue in the previous conjunction.
        /// </summary>
        public void Search()
        {
            int[] lastUsedIndeces = new int[pattern.PatternCount];
            lastUsedIndeces.Populate(-1);

            int lastUsedIndex = -1;

            while (true)
            {
                // -1 meaning that next conjunction search will start from the beginning of vertix list. 
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
        /// Initiates iteration over one connected pattern.
        /// We try to start search from each vertex in the graph.
        /// Algorithm is divided into filling up chains.
        /// Once one chain is filled/depleted we return from main search loop.
        /// Filled: We check whether next pattern in part of the conjuntion or there is another conjunction.
        /// If there is another conjunction we return the index of the last used vertex that will be later used to init this conjunction search.
        /// Otherwise we prepare next sub pattern chain and pick an element that connects that pattern, it will be immediately matched.
        /// Depleted: It is check if current pattern chain is starting point of a conjunction, if it is we either pick next vertex from the graph
        /// or we just return -1, that is we finished searching this conjunction.
        /// If it is not the starting point, we simply prepare previous sub pattern and set next element to null,
        /// which will lead to dfs backwards once the main loop is started.
        /// </summary>
        /// <param name="lastIndex"> Last index from last iteration. </param>
        /// <param name="cameFromUp"> If we came from a different conjunction. </param>
        /// <returns> Last used index. </returns>
        public int DFSStartOfCunjunction(int lastIndex, bool cameFromUp)
        {
            var vertices = this.PickConjunctionStartingVertices();
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
                    }
                    else
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
        /// <returns> True if there is another pattern (Filled), False if it is returning.(Empty) </returns>
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
                            // Setting null here makes it to fail on next iteration and it is forced to dfs back.
                            result.Print();
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
                        // The 0th variable must be unset here, because we cant anticipate if we failed on 0th match or somewhere else.
                        this.ClearCurrentFromResult();
                        pattern.PreparePreviousNode();
                        break;
                    }
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
                return ((Edge)lastUsedElement).EndVertex;
            }
        }


        /// <summary>
        /// Processing Vertex:
        /// We are returning from the dfs.
        /// When processing the vertex, we failed to add the vertex to the pattern, 
        /// that means we need to go down in the pattern and also remove the edge we came from to the vertex. 
        /// In order to do so, we will return null, next loop in algorithm fails on adding edge, so the edge gets removed.
        /// 
        /// Processing Edge:
        /// When processing edge, we get the last used edge from the result and remove it from result.
        /// (Note there can be no edge, eg: we failed to add one at all.)
        /// We take the edge (null or normal edge) and try to do dfs from the vertex the edge started from 
        /// If it returns a new edge we can continue trying to apply the edge on the same index in pattern.
        /// If it is null we need to remove also the vertex because there are no more available edges from this vertex.
        /// In order to do that we go down in pattern and return null, so the algorithm fail 
        /// on adding vertex so it jumps here again and so on.
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
                // Take the edge on the current position. (Edge that was matched before, can be null if no edge was there.)
                Element lastUsedEdgeInResult = (Edge)result[pattern.OverAllIndex];

                // lastElement is null only when we are returning from the removed vertex, we take the last used edge in the result.
                // Else we always use the newest edge we failed on. 
                if (lastElement == null) lastElement = lastUsedEdgeInResult;

                // Clears the last used edge from result, or does nothing (no edge was there).
                // It needs to clean the scope so that it can apply next possible edge if one is found.
                ClearCurrentFromResult();
                pattern.UnsetCurrentVariable();


                // Try to find new edge from the last vertex.
                processingVertex = true; // To jump into dfs forward.
                Element nextElement =
                    DoDFSForward((Vertex)result[pattern.OverAllIndex - 1], (Edge)lastElement);

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
        /// Fixed errors when returning to from another pattern which caused using different edge types and infinite loop.
        /// Notice that this method is called only when matching type Any of the edge.
        /// </summary>
        /// <param name="vertex"> Edges of the vertex will be searched for next possible. </param>
        /// <param name="lastUsedEdge"> Last used edge of the vertex. </param>
        /// <returns> Next edge of the vertex. </returns>
        private Edge FindAnyEdge(Vertex vertex, Edge lastUsedEdge)
        {
            Edge nextEdge = null;
            if (lastUsedEdge == null || lastUsedEdge.EdgeType == EdgeType.InEdge)
            {
                nextEdge = FindInEdge(vertex, lastUsedEdge);
                if (nextEdge == null) lastUsedEdge = null;
                else return nextEdge;
            }
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
            result[pattern.OverAllIndex] = element;
        }

        /// <summary>
        /// Removes the last element from the result.
        /// </summary>
        private void ClearCurrentFromResult()
        {
            result[pattern.OverAllIndex] = null;
        }

        private List<Vertex> PickConjunctionStartingVertices()
        {
            if (this.pattern.CurrentPatternIndex == 0 && 
                (this.pattern.CurrentMatchNodeIndex == 0 || this.pattern.isLastNodeInCurrentPattern()))
                return this.startingVertices;
            else return this.graph.vertices;
        }

        public void SetStartingVertices(List<Vertex> vertices)
        {
            if (vertices == null || vertices.Count == 0) 
                throw new ArgumentException($"{this.GetType()}, starting vertices are empty on thread {Thread.CurrentThread.ManagedThreadId} .");
            else this.startingVertices = vertices;
        }
    }





    /// <summary>
    /// Serves as a paraller searcher. Contains threads and matchers.
    /// </summary>
    class DFSParallelPatternMatcher : IParallelMatcher
    {
        Thread[] Threads;
        ISingleThreadMatcher[] Matchers;
        Graph Graph;

        public DFSParallelPatternMatcher(IDFSPattern pattern, Graph graph, QueryResults results)
        {
            this.Graph = graph;
            this.Threads = new Thread[QueryEngine.ThreadsPerQuery];
            this.Matchers = new ISingleThreadMatcher[QueryEngine.ThreadsPerQuery];
            
            for (int i = 0; i < QueryEngine.ThreadsPerQuery; i++)
            {
                this.Threads[i] = new Thread(DFSParallelPatternMatcher.Work);

                this.Matchers[i] = (ISingleThreadMatcher)MatchFactory
                                   .CreateMatcher("DFSSingleThread",                // Type of Matcher 
                                                  i==0 ? pattern : pattern.Clone(), // Cloning of pattern (one was already created)
                                                  graph,                            
                                                  results, 
                                                  i);                               // Index where to store thread results
                //
            }
        }


        /// <summary>
        /// Creates jobs for threads and starts them. 
        /// The main thread waits for all the searchers to finish.
        /// </summary>
        public void Search()
        {
            // creation of jobs, seting threads to run and waiting to finish
            var distributor = new VertexDistributor(this.Graph.GetAllVertices(), 3);

            //Create jobs and assign them to threads and run the thread.
            for (int i = 0; i < this.Threads.Length; i++)
            {
                Job tmpJob = new Job(distributor, this.Matchers[i]);
                this.Threads[i].Start(tmpJob);
            }

            // Wait for all working threads.
            for (int i = 0; i < this.Threads.Length; i++)
            {
                this.Threads[i].Join();
            }

        }


        /// <summary>
        /// Method passed to a thread.
        /// A thread asks for a new starting vertices for his matcher.
        /// If there are no more vertices the method ends.
        /// </summary>
        /// <param name="o"> Class containing matcher and distributor. </param>
        private static void Work(object o)
        {
            Job job = (Job)o;

            if (QueryEngine.ThreadsPerQuery == 1)
            {
                job.Matcher.Search(); // Starting vertices implicitly set to entire graph.
            } else
            {
                while (true)
                {
                    List<Vertex> startingVertices = null;

                    lock (job.Distributor)
                    {
                        startingVertices = job.Distributor.DistributeVertices();
                    }

                    if (startingVertices == null) break;
                    else
                    {
                        job.Matcher.SetStartingVertices(startingVertices);
                        job.Matcher.Search();
                    }
                }
            }
        }

        /// <summary>
        /// A Class serves as a parameter to paramethrisised method passed to a thread. 
        /// </summary>
        private class Job 
        {
            public VertexDistributor Distributor;
            public ISingleThreadMatcher Matcher;

            public Job(VertexDistributor vD, ISingleThreadMatcher m)
            {
                this.Distributor = vD;
                this.Matcher = m;
            }
        }


        /// <summary>
        /// Classes serves as a distributor of vertices to threads.
        /// Each thread will be given certain amount of vertices to process.
        /// Working with this class is critical section where multiple threads can meet.
        /// Locking should be done.
        /// </summary>
        private class VertexDistributor
        {
            List<Vertex> Vertices;
            int VerticesPerRound;
            int FirstFreeIndex;

            public VertexDistributor(List<Vertex> vertices, int verticesPerRound)
            {
                if (vertices == null || vertices.Count == 0) 
                    throw new ArgumentException($"{this.GetType()} creating with 0 vertices.");
                else this.Vertices = vertices;
                
                if (verticesPerRound <= 0) 
                    throw new ArgumentException($"{this.GetType()} creating with 0 rounds.");
                else this.VerticesPerRound = verticesPerRound;
                
                this.FirstFreeIndex = 0;
            }


            /// <summary>
            /// Method is called from within Work inside each thread.
            /// Always returns portion of graph vertices.
            /// </summary>
            /// <returns> Null if no more vertices to distribute or list of vertices from a graph.</returns>
            public List<Vertex> DistributeVertices()
            {
                List<Vertex> distributedVertices = null;

                // No more vertices to distribute
                if (this.FirstFreeIndex >= this.Vertices.Count) return null;
                // We can distribute last portion of vertices
                else if (this.FirstFreeIndex + this.VerticesPerRound >= this.Vertices.Count)
                {
                    distributedVertices = this.Vertices.GetRange(this.FirstFreeIndex, this.Vertices.Count - this.FirstFreeIndex);
                    this.FirstFreeIndex += this.VerticesPerRound;
                } else // There are still more vertices to distribute 
                {
                    distributedVertices = this.Vertices.GetRange(this.FirstFreeIndex, this.VerticesPerRound);
                    this.FirstFreeIndex += this.VerticesPerRound;
                }

                return distributedVertices;
            }
        }


    }

}
