using System;
using System.Collections.Generic;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// A class serves as a distributor of vertices from graph to threads.
    /// Each thread that calls this method will be given certain amount of vertices to process.
    /// Working with this class is critical section where multiple threads can meet.
    /// </summary>
    internal sealed class VertexDistributor
    {
        List<Vertex> vertices;
        /// <summary>
        /// A number of vertices to give during vertex distribution method call.
        /// </summary>
        readonly int verticesPerRound;
        /// <summary>
        /// The index of the vertex that has not been distributed yet in the graph.
        /// </summary>
        int nextFreeIndex;

        /// <summary>
        /// Creates a vertex distributor.
        /// </summary>
        /// <param name="vertices"> All vertices from a graph. </param>
        /// <param name="verticesPerRound"> A number of vertices to distribute to a thread on demand.</param>
        public VertexDistributor(List<Vertex> vertices, int verticesPerRound)
        {
            if (vertices == null || vertices.Count == 0 || verticesPerRound <= 0)
                throw new ArgumentException($"{this.GetType()} creating with 0 vertices or empty rounds.");
            else
            {
                this.verticesPerRound = verticesPerRound;
                this.vertices = vertices;
            }
        }


        /// <summary>
        /// The method is called from within Work inside each thread.
        /// Always returns a range of graph vertices.
        /// To omit locking, there is an atomic operation.
        /// On call the it receives end index of the returned range.
        /// The value is then substracted to obtain the start of the range.
        /// Because we obtains the end index, the lock can be ommited and thread it self can decide
        /// whether to continue in the search or not.
        /// The search ends if the range exceeds the count of vertices in the graph.
        /// </summary>
        /// <returns> A starting index and an ending index of a round or start/end set to -1 for no more vertices to be distribute.</returns>
        public void DistributeVertices(ref int start, ref int end)
        {
            int tmpEndOfRound = Interlocked.Add(ref this.nextFreeIndex, this.verticesPerRound);
            int tmpStartOfRound = tmpEndOfRound - this.verticesPerRound;

            // The first index is beyond the size of the array of vertices -> no more vertices to distribute.
            if (tmpStartOfRound >= this.vertices.Count)
            {
                start = -1;
                end = -1;

            }  // Return all vertices to the end of the list. 
               // Returned range is smaller than the round size because there is not enough vertices. 
            else if (tmpEndOfRound >= this.vertices.Count)
            {
                start = tmpStartOfRound;
                end = this.vertices.Count;

            } // Return a normal size range.
            else
            {
                start = tmpStartOfRound;
                end = tmpEndOfRound;
            }
        }
    }
}
