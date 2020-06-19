/*! \file
  
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
using System.ComponentModel.Design;

namespace QueryEngine
{
    /// <summary>
    /// Serves as a paraller searcher. Contains threads and matchers.
    /// Class contains definitions of jobs for threads and vertex distributor.
    /// If only one thread is used for matching the single thread variant is used otherwise the multithread variant is used.
    /// </summary>
    sealed class DFSParallelPatternMatcher : IParallelMatcher
    {
        Thread[] Threads;
        ISingleThreadMatcher[] Matchers;
        Graph Graph;
        int DistributorVerticesPerRound;
        MatchResultsStorage Results;

        /// <summary>
        /// Creates a parallel matchers.
        /// Inits arrays of threads and matchers based on thread count.
        /// </summary>
        /// <param name="pattern"> Pattern to match. </param>
        /// <param name="graph"> Graph to search on.</param>
        /// <param name="results"> Where to store results. </param>
        /// <param name="threadCount"> Number of threads to search.</param>
        /// <param name="verticesPerThread"> If more than one thread is used to search this defines number of vertices that will be distributed to threads during matching.</param>
        public DFSParallelPatternMatcher(IDFSPattern pattern, Graph graph, MatchResultsStorage results, int threadCount, int verticesPerThread = 1)
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
                this.Matchers[i] = (ISingleThreadMatcher)MatchFactory
                                   .CreateMatcher("DFSSingleThread",                  // Type of Matcher 
                                                  i == 0 ? pattern : pattern.Clone(), // Cloning of pattern (one was already created)
                                                  graph,
                                                  results,
                                                  i);                                 // Index where to store thread results
            }
        }

        /// <summary>
        /// Initiates search on graph.
        /// There are two possibilities as it can go.
        /// Based on number of threads. Either the main thread does the search it self, if
        /// only one thread can be used. Or parallel search is used if more threads can be used.
        /// Parallel search work in two steps, first step is the graph search where each thread 
        /// saves its results into separate bins (however, still inside one result structure). The bins are 
        /// accessed via indices that were given to matchers instances. When the matchers finish, the results are then
        /// parallelly merged into one bin.
        /// </summary>
        public void Search()
        {
            QueryEngine.stopwatch.Start();

            if (this.Threads.Length == 1) this.Matchers[0].Search();
            else
            {
                this.ParallelSearch();

                TimeSpan tss = QueryEngine.stopwatch.Elapsed;
                string eelapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", tss.Hours, tss.Minutes, tss.Seconds, tss.Milliseconds / 10);
                Console.WriteLine("Query time " + eelapsedTime);

                this.ParallelMergeThreadResults();
            }

            TimeSpan ts = QueryEngine.stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Query + merge time " + elapsedTime);
        }

        /// <summary>
        /// Class serves as a signal to the main thread that all its spawned threads has finished it is work.
        /// </summary>
        private class Synchronizer
        {
            public readonly object lockingObject = new object();
            int workThreads;
            int finishedThreads;


            /// <summary>
            /// Creates synchronizer with number of threads to wait for.
            /// </summary>
            /// <param name="workThreads"> Number of threads to wait for. </param>
            public Synchronizer(int workThreads)
            {
                this.workThreads = workThreads;
                this.finishedThreads = 0;
            }

            /// <summary>
            /// Signals that the calling thread finished its work.
            /// If it is the last thread, it signals the main app thread that it can continue.
            /// </summary>
            public void SignalFinish()
            {
                var tmpFinished = Interlocked.Increment(ref this.finishedThreads);
                if (tmpFinished == this.workThreads)
                {
                    lock (lockingObject)
                    {
                       Monitor.Pulse(lockingObject);
                    }
                }
            } 
        }



        // This section contains structures and algorithm for handling parallel matching.
        #region ParalelSearch

        /// <summary>
        /// Initiates parallel search.
        /// Main thread spawns threads that have their own matchers. The threads share a common structure,
        /// a vertex distributor. Each thread asks for a portion of vertices to iterate over and when the iteration is over,
        /// they ask for more vertices. If there are no more vertices the threads finish and signals the main thread that is 
        /// waiting for the Pulse from Synchronizer created in this method.
        /// </summary>
        private void ParallelSearch()
        {
            var thisSynchronizer = new Synchronizer(this.Threads.Length);
            var distributor = new VertexDistributor(this.Graph.GetAllVertices(), this.DistributorVerticesPerRound);

            // We are locking before starting the thread to avoid misshappen if all threads finished before
            // the main thread reached the Wait method.
            lock (thisSynchronizer.lockingObject)
            {
                for (int i = 0; i < this.Threads.Length; i++)
                {
                    this.Threads[i] = new Thread(DFSParallelPatternMatcher.WorkMultiThreadSearch);
                    this.Threads[i].Priority = ThreadPriority.Highest;
                    this.Threads[i].Start(new JobMultiThreadSearch(thisSynchronizer, distributor, this.Matchers[i]));
                }

                // Wait for all working threads to finish.
                Monitor.Wait(thisSynchronizer.lockingObject);
            }
        }

        /// <summary>
        /// Method passed to threads.
        /// A thread asks for a new starting vertices for his matcher from a vertex distributor.
        /// If there are no more vertices the method ends.
        /// </summary>
        /// <param name="o"> Class containing matcher and distributor and synchrinizer for the main thread. </param>
        private static void WorkMultiThreadSearch(object o)
        {
            JobMultiThreadSearch job = (JobMultiThreadSearch)o;

            int start = 0;
            int end = 0;
            while (true)
            {
                // Ask for more vertices to iterate over.
                job.Distributor.DistributeVertices(ref start, ref end);

                // No more vertices. The thread can end.
                if (start == -1 || end == -1)
                {
                    job.MainThreadSynchronizer.SignalFinish();
                    break;
                }
                else
                {
                    // Set the range of vertices to the matcher and start searching the graph.
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
            public Synchronizer MainThreadSynchronizer;

            public JobMultiThreadSearch(Synchronizer synchronizer, VertexDistributor vertexDistributor, ISingleThreadMatcher matcher)
            {
                this.MainThreadSynchronizer = synchronizer;
                this.Distributor = vertexDistributor;
                this.Matcher = matcher;
            }
        }

        /// <summary>
        /// Classes serves as a distributor of vertices from graph to threads.
        /// Each thread that calls this method will be given certain amount of vertices to process.
        /// Working with this class is critical section where multiple threads can meet.
        /// </summary>
        private class VertexDistributor
        {
            List<Vertex> Vertices;
            /// <summary>
            /// Number of vertices to give during vertex distribution method call.
            /// </summary>
            int VerticesPerRound;
            /// <summary>
            /// The index of the vertex that has not been distributed yet in the graph.
            /// </summary>
            int NextFreeIndex;

            /// <summary>
            /// Creates a vertex distributor.
            /// </summary>
            /// <param name="vertices"> All vertices from a graph. </param>
            /// <param name="verticesPerRound"> Number of vertices to distribute to a thread on demand.</param>
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
            /// Always returns range of graph vertices.
            /// To omit locking, there is an atomic operation.
            /// On call the it receives end index of the returned range.
            /// The value is then substracted to obtain the start of the range.
            /// Because we obtains the end index, the lock can be ommited and thread it self can decide
            /// whether to continue in the search or not.
            /// The search ends if the range exceeds the count of vertices in the graph.
            /// </summary>
            /// <returns> Starting index and ending index of a round or start/end set to -1 for no more vertices to be distribute.</returns>
            public void DistributeVertices(ref int start, ref int end)
            {
                int tmpEndOfRound = Interlocked.Add(ref this.NextFreeIndex, this.VerticesPerRound);
                int tmpStartOfRound = tmpEndOfRound - this.VerticesPerRound;

                // First index is beyond the size of the array of vertices -> no more vertices to distribute.
                if (tmpStartOfRound >= this.Vertices.Count)
                {
                    start = -1;
                    end = -1;

                }  // Return all vertices to the end of the list. 
                   // Returned range is smaller than the round size because there is not enough vertices. 
                else if (tmpEndOfRound >= this.Vertices.Count)
                {
                    start = tmpStartOfRound;
                    end = this.Vertices.Count;

                } // Return normal size range.
                else
                {
                    start = tmpStartOfRound;
                    end = tmpEndOfRound;
                }
            }
        }
       
        #endregion ParalelSearch

 
        
        
        // This section contains structures for parallel merging of results.
        #region ParalelMerge

        /// <summary>
        /// Merges results from all threads into one list. Note that the number of rows to merge is the same number
        /// as the number of threads.
        /// Merging can be done in two ways. If the half of available threads is larger than the number of columns.
        /// The first method is used.
        /// The half of the threads is assigned rows to merge. They merge it in parallel and then they wait for them to finish.
        /// When they finish, the number rows to be merged is now twice smaller. The last running thread redistributes the newly
        /// merged rows to the half of threads used for the first merging. The rest of threads finish. The same repeats until no more
        /// rows can be merged.
        /// The second method does parallel column mergins, instead of mergin columns, it assignes columns to threads that merge the entire
        /// columns into one column.
        /// </summary>
        private void ParallelMergeThreadResults()
        {
             if (this.Threads.Length / 2 > this.Results.ColumnCount)
                MergeRow();
             else
                MergeColumn();

        }

        #region MergeColumn

        /// <summary>
        /// Merges columns of the result table in parallel.
        /// Each thread askes for a column to merge from a column distributor.
        /// If there are no more columns to merge, the thread finishes.
        /// </summary>
        private void MergeColumn()
        {
            var columnDistributor = new ColumnDistributor(this.Results.ColumnCount);
            int threadsToUse = (this.Threads.Length < this.Results.ColumnCount ?
                                this.Threads.Length : this.Results.ColumnCount);
            var thisSynchronizer = new Synchronizer(threadsToUse);

            lock (thisSynchronizer.lockingObject)
            {
                for (int i = 0; i < threadsToUse; i++)
                {
                    this.Threads[i] = new Thread(DFSParallelPatternMatcher.ParallelMergeColumnWork);
                    this.Threads[i].Priority = ThreadPriority.Highest;
                    this.Threads[i].Start(new ParallelMergeColumnJob(thisSynchronizer, columnDistributor, this.Results));
                }
                Monitor.Wait(thisSynchronizer.lockingObject);
            }
        }
        
        /// <summary>
        /// Argument to a thread.
        /// Tries to get a free column index and on successful retrieval of the index.
        /// The column is merged.
        /// </summary>
        /// <param name="o"> Merge Job.</param>
        private static void ParallelMergeColumnWork(object o)
        {
            ParallelMergeColumnJob job = (ParallelMergeColumnJob)o;

            int columnIndex;
            while (true)
            {
                // Ask for a column to distribute.
                columnIndex = job.ColumnDistributor.DistributeColumn();
                if (columnIndex == -1)
                {   // No more columns -> the thread can end.
                    job.MainThreadSynchronizer.SignalFinish();
                    break;
                }
                else job.Elements.MergeColumn(columnIndex);
            }
        }

        /// <summary>
        /// Passes as an argument to a paralel merge work.
        /// </summary>
        private class ParallelMergeColumnJob
        {
            public Synchronizer MainThreadSynchronizer;
            public MatchResultsStorage Elements;
            public ColumnDistributor ColumnDistributor;
            public ParallelMergeColumnJob(Synchronizer synchronizer, ColumnDistributor columnDistributor, MatchResultsStorage elements)
            {
                this.MainThreadSynchronizer = synchronizer;
                this.Elements = elements;
                this.ColumnDistributor = columnDistributor;
            }
        }

        /// <summary>
        /// The class serves as a work distributor to running threads that merge results from the result table.
        /// The threads call method to distribute column indeces that the threads will merge in parallel.
        /// </summary>
        private class ColumnDistributor
        {
            /// <summary>
            /// Number of columns that have been disributed.
            /// </summary>
            int firstFreeColumn = 0;
            /// <summary>
            /// Number of columns to distribute.
            /// </summary>
            int columnCount; 

            public ColumnDistributor(int columnCount)
            {
                this.columnCount = columnCount;
            }

            /// <summary>
            /// Distributes a free column index to merge.
            /// The method uses interlock atomic operation to avoid  using lock.
            /// It atomicaly increments the number of free column index.
            /// Then it substract the position and ther thread can decide whether to finish
            /// because all columns have been merged.
            /// </summary>
            /// <returns> Index of a column to merge or -1 on no more columns. </returns>
            public int DistributeColumn()
            {
                int tmpNextFreeColumn = Interlocked.Increment(ref this.firstFreeColumn);
                int tmpFirstFreeColumn = tmpNextFreeColumn - 1;

                if (tmpFirstFreeColumn < columnCount) return tmpFirstFreeColumn;
                else return -1;
            }

        }

        #endregion MergeColumn

        #region MergeRow


        /// <summary>
        /// Merges results from parallel search.
        /// </summary>
        private void MergeRow()
        {
            Thread thread = new Thread(() => DFSParallelPatternMatcher.ParallelMergeRowWork(this.Results, 0, this.Threads.Length));
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Each call the method receives a range to merge. If the range is smaller or equal three, it merges the results into the
        /// row on position at value of first. If the range is larger it can be split into another thread to work by calculating middle position.
        /// If the middle is on an odd number, the middle is moved one position back. What that means is that if we want to split range of 10,
        /// middle is 5, and ranges 0 5, 5 10 are worked on separately, it would continue into 0 2, 2 5, 5 7, 7 9 merges. Which ommits one possible merge, which 
        /// results in merging two times 3 rows.
        /// Each merge is stored to the position at the value of first.
        /// </summary>
        /// <param name="results"> Result table to merge.</param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private static void ParallelMergeRowWork(MatchResultsStorage results, int start, int end)
        {
            // do work parallel
            if (end - start > 3)
            {
                // compute middle of the range
                int middle = ((end - start) / 2) + start;
                if (middle % 2 == 1) middle--;

                Thread thread = new Thread(() => DFSParallelPatternMatcher.ParallelMergeRowWork(results, middle, end));
                thread.Priority = ThreadPriority.Highest;
                thread.Start();

                DFSParallelPatternMatcher.ParallelMergeRowWork(results, start, middle);
                
                // Wait for other thread to finish and start merging its results with yours.
                thread.Join();
                results.MergeRows(start, middle);
            } else
            { // merge rows
                for (int i = start + 1; i < end; i++)
                    results.MergeRows(start, i);
            }
        }

        #endregion MergeRow

        #endregion ParalelMerge
    }
}
