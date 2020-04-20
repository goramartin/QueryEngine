﻿/*! \file
  
  This class includes definitions of dfs search paralel algorithm used to find pattern defined in query match expression.
  
  This paralel version only uses single threaded version of the dfs search algorithm.
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
using System.Threading;
using System.Diagnostics;

namespace QueryEngine
{
    /// <summary>
    /// Serves as a paraller searcher. Contains threads and matchers.
    /// Class contains definitions of jobs for threads and vertex distributor.
    /// If only one thread is used for matching the single thread variant is used otherwise the multithread variant is used.
    /// </summary>
    class DFSParallelPatternMatcher : IParallelMatcher
    {
        Thread[] Threads;
        ISingleThreadMatcher[] Matchers;
        Graph Graph;
        int DistributorVerticesPerRound;
        IMatchResultStorage Results;

        /// <summary>
        /// Creates a parallel matchers.
        /// Inits arrays of threads and matchers based on thread count.
        /// </summary>
        /// <param name="pattern"> Pattern to match. </param>
        /// <param name="graph"> Graph to search on.</param>
        /// <param name="results"> Where to store results. </param>
        /// <param name="threadCount"> Number of threads to search.</param>
        /// <param name="verticesPerThread"> If more than one thread is used to search this defines number of vertices that will be distributed to threads during matching.</param>
        public DFSParallelPatternMatcher(IDFSPattern pattern, Graph graph, IMatchResultStorage results, int threadCount, int verticesPerThread = 1)
        {
            if (threadCount <= 0 || verticesPerThread <= 0)
                throw new ArgumentException($"{this.GetType()}, invalid number of threads or vertices per thread.");

            this.DistributorVerticesPerRound = verticesPerThread;
            this.Graph = graph;
            this.Threads = new Thread[threadCount];
            this.Matchers = new ISingleThreadMatcher[threadCount];
            this.Results = results;

            for (int i = 0; i < threadCount; i++)
            {
                if (threadCount > 1)
                    this.Threads[i] = new Thread(DFSParallelPatternMatcher.WorkMultiThread);
                else this.Threads[i] = new Thread(DFSParallelPatternMatcher.WorkSingleThread);

                this.Matchers[i] = (ISingleThreadMatcher)MatchFactory
                                   .CreateMatcher("DFSSingleThread",                // Type of Matcher 
                                                  i == 0 ? pattern : pattern.Clone(), // Cloning of pattern (one was already created)
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
            if (this.Threads.Length == 1)
            {
                JobSingleThreadSearch tmpJob = new JobSingleThreadSearch(this.Matchers[0]);
                this.Threads[0].Start(tmpJob);

            }
            else
            {
                // creation of jobs, seting threads to run and waiting to finish
                var distributor = new VertexDistributor(this.Graph.GetAllVertices(), this.DistributorVerticesPerRound);
                //Create jobs and assign them to threads and run the thread.
                for (int i = 0; i < this.Threads.Length; i++)
                {
                    JobMultiThreadSearch tmpJob = new JobMultiThreadSearch(distributor, this.Matchers[i]);
                    this.Threads[i].Start(tmpJob);
                }
            }

            // Wait for all working threads.
            for (int i = 0; i < this.Threads.Length; i++)
            {
                this.Threads[i].Join();
                this.Matchers[i] = null;
                this.Threads[i] = null;
            }

            // Merge Arrays with results
            MergeThreadResults();

        }


        #region ParalelMerge

        /// <summary>
        /// Merges results from all threads into a one list.
        /// Merging is done by merging entire one column, that is to say, all results from all threads 
        /// in the column are merged into the first thread position in the column.
        /// </summary>
        private void MergeThreadResults()
        {
            if (this.Threads.Length == 1) return;
            else
            {
                var columnDistributor = new ColumnDistributor(this.Results.ColumnCount);
                int threadsToUse = (this.Threads.Length < this.Results.ColumnCount ? this.Threads.Length : this.Results.ColumnCount);
                for (int i = 0; i < threadsToUse; i++)
                {
                    this.Threads[i] = new Thread(DFSParallelPatternMatcher.MergeWork);
                    this.Threads[i].Start(new MergeJob(columnDistributor, this.Results));
                }

                for (int i = 0; i < threadsToUse; i++)
                    this.Threads[i].Join();
            }
        }

        /// <summary>
        /// Argument to a thread.
        /// Tries to get a free column index and on successful retrieval of the index.
        /// The column is merged.
        /// </summary>
        /// <param name="o"> Merge Job.</param>
        private static void MergeWork(object o)
        {
            MergeJob mergeJob = (MergeJob)o;

            int columnIndex;
            while (true)
            {
                columnIndex = mergeJob.ColumnDistributor.DistributeColumn();
                if (columnIndex == -1) break;
                else  mergeJob.Elements.MergeColumn(columnIndex);
            }
        }

        /// <summary>
        /// Passes as an argument to a paralel merge work.
        /// </summary>
        private class MergeJob
        {
            public IMatchResultStorage Elements;
            public ColumnDistributor ColumnDistributor;
            public MergeJob(ColumnDistributor columnDistributor, IMatchResultStorage elements)
            {
                this.Elements = elements;
                this.ColumnDistributor = columnDistributor;
            }
        }

        /// <summary>
        /// Distributes columns to merge.
        /// Each distribution encomapasses an index of a column.
        /// The column in the given index will be merged.
        /// </summary>
        private class ColumnDistributor
        {
            int firstFreeColumn = 0;
            int columnCount;

            public ColumnDistributor(int columnCount)
            {
                this.columnCount = columnCount;
            }

            /// <summary>
            /// Distributes a free column index to merge.
            /// </summary>
            /// <returns> Index of a column to merge or -1 on no more columns. </returns>
            public int DistributeColumn()
            {
                int tmpNextFreeColumn = Interlocked.Add(ref this.firstFreeColumn, 1);
                int tmpFirstFreeColumn = tmpNextFreeColumn - 1;

                if (tmpFirstFreeColumn < columnCount) return tmpFirstFreeColumn;
                else return -1;
            }

        }

        #endregion ParalelMerge

        #region ParalelSearch

        /// <summary>
        /// Method passed to a thread.
        /// Matcher implicitly iterates over entire graph.
        /// </summary>
        /// <param name="o"> Class containing matcher. </param>
        private static void WorkSingleThread(object o)
        {
            JobSingleThreadSearch job = (JobSingleThreadSearch)o;
            job.Matcher.Search(); // Starting indeces implicitly set to entire graph.
        }


        /// <summary>
        /// Method passed to threads.
        /// A thread asks for a new starting vertices for his matcher.
        /// If there are no more vertices the method ends.
        /// </summary>
        /// <param name="o"> Class containing matcher and distributor. </param>
        private static void WorkMultiThread(object o)
        {
            JobMultiThreadSearch job = (JobMultiThreadSearch)o;

            while (true)
            {
                int start;
                int end;

                lock (job.Distributor)
                {
                    job.Distributor.DistributeVertices(out start, out end);
                }

                if (start == -1 || end == -1) break;
                else
                {
                    job.Matcher.SetStartingVerticesIndeces(start, end);
                    job.Matcher.Search();
                }
            }

        }

        /// <summary>
        /// A Class serves as a parameter to paramethrisised method passed to a thread.
        /// Contains vertex distributor and matcher.
        /// Used when multiple threads can be used to search graph.
        /// </summary>
        private class JobMultiThreadSearch
        {
            public VertexDistributor Distributor;
            public ISingleThreadMatcher Matcher;

            public JobMultiThreadSearch(VertexDistributor vD, ISingleThreadMatcher m)
            {
                this.Distributor = vD;
                this.Matcher = m;
            }
        }


        /// <summary>
        /// A Class serves as a parameter to paramethrisised method passed to a thread.
        /// Contains matcher.
        /// Used when a single thread is used to search graph.
        /// </summary>
        private class JobSingleThreadSearch
        {
            public ISingleThreadMatcher Matcher;

            public JobSingleThreadSearch(ISingleThreadMatcher m)
            {
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
            int VerticesPerRound = 3;
            int NextFreeIndex;

            public VertexDistributor(List<Vertex> vertices, int verticesPerRound)
            {
                if (vertices == null || vertices.Count == 0 || verticesPerRound <= 0)
                    throw new ArgumentException($"{this.GetType()} creating with 0 vertices or empty rounds.");
                else
                {
                    this.VerticesPerRound = verticesPerRound;
                    this.Vertices = vertices;
                }

            }


            /// <summary>
            /// Method is called from within Work inside each thread.
            /// Always returns portion of graph vertices.
            /// </summary>
            /// <returns> Starting index and ending index of a round or start/end set to -1 is no more vertices are to be distribute.</returns>
            public void DistributeVertices(out int start, out int end)
            {
                int tmpEndOfRound = Interlocked.Add(ref this.NextFreeIndex, this.VerticesPerRound);
                int tmpStartOfRound = tmpEndOfRound - this.VerticesPerRound;

                // First index is beyond the size of the array of vertices.
                if (tmpStartOfRound >= this.Vertices.Count)
                {
                    start = -1;
                    end = -1;
                    // Return all vertices to the end of the list. Returned round is smaller because there is not enough vertices. 
                }
                else if (tmpEndOfRound >= this.Vertices.Count)
                {
                    start = tmpStartOfRound;
                    end = this.Vertices.Count;
                    // Return normal size round.
                }
                else
                {
                    start = tmpStartOfRound;
                    end = tmpEndOfRound;
                }

                Debug.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + " asked for vertices and gets start = " + start + " , end = " + end + ".");
            }
        }
       
        #endregion ParalelSearch

    }
}
