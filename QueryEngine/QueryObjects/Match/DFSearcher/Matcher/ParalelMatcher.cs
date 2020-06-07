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
                this.ParallelMergeThreadResults();
            }

            for (int i = 0; i < this.Matchers.Length; i++)
                QueryEngine.countXX += ((DFSPatternMatcher)this.Matchers[i]).count;

            Console.WriteLine(QueryEngine.countXX);
            TimeSpan ts = QueryEngine.stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
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
        /// Initiates a parallel merging of results by rows.
        /// The number of threads used during matching define the number of rows to merge.
        /// The half of threads used for searching is taken and each thread merges two rows in the result table.
        /// After they merge the rows, only half of threads from the first merging continues to the second round.
        /// Where they merge the rows merged from before. Each round the number of rows is reducced by half.
        /// When the number of rows to merge reaches 1 the algirthm finishes.
        /// Threads exluded from merging finished at the time when round ends.
        /// The threads are given jobs that contain index that decides whether the thread will continue into the next round.
        /// For example:
        /// The number of threads for search is 10, then the number of rows to merge is 10 and threads to use for merging the first 
        /// round is 5. With indeces of jobs ordered 1 2 3 4 5. They are given the row numbers to merge and they start merging.
        /// The rows are taken as first and the last, the second and the last-1 ... here are touples (0,9) (1,8) (2,7)...
        /// If the number of rows is odd, the thread that gets touple (0, last) also merges (0,last-1) and the other touples are shifted by 
        /// 2 instead of 1.
        /// When one thread finishes it checks whether it is the last thread working and its index if it continues to the next round.
        /// If the thread is last, it redistributes rows to a waiting threads via their jobs. Then they continue mergin while 
        /// threads with indeces higher then 5/2=2 are discarded. If it is not the last one working it either stops working or it waits
        /// for a pulse from the last wokring thread.
        /// Note that the thread with index 1 always is the last one alive when everything is merged.
        /// </summary>
        private void MergeRow()
        {
            var thisSynchronizer = new Synchronizer(this.Threads.Length / 2);
            var rowDistributor = new RowDistributor(this.Threads.Length / 2, this.Threads.Length);

            lock (thisSynchronizer.lockingObject)
            {
                for (int i = 0; i < this.Threads.Length / 2; i++)
                {
                    this.Threads[i] = new Thread(DFSParallelPatternMatcher.ParallelMergeRowWork);
                    var tmp = new ParallelMergeRowJob(i+1, thisSynchronizer, rowDistributor, this.Results);
                    rowDistributor.AddJob(tmp);

                    // Distribute verices 
                    tmp.FirstRow = i;
                    if (i != 0 && this.Threads.Length % 2 == 1)
                        tmp.SecondRow = this.Threads.Length - i - 2;
                    else tmp.SecondRow = this.Threads.Length - i - 1;

                    this.Threads[i].Start(tmp);
                }
                Monitor.Wait(thisSynchronizer.lockingObject);
            }
            Console.WriteLine("finished");
        }

        /// <summary>
        /// A work passes to a thread.
        /// It merges rows from a job, then it signals that it finished.
        /// </summary>
        /// <param name="o"></param>
        private static void ParallelMergeRowWork(object o)
        {
            ParallelMergeRowJob job = (ParallelMergeRowJob)o;

            bool canContinue = true;
            while (true)
            {
                if (canContinue)
                {
                    job.Elements.MergeRows(job.FirstRow, job.SecondRow);
                    // Case of odd number of rows and (0, last) tuple.       
                    if (job.FirstRow == 0 && (job.SecondRow % 2 == 0)) 
                           job.Elements.MergeRows(job.FirstRow, job.SecondRow - 1);
                }
                else
                {
                    Console.WriteLine("Finished with job = " + job.ThreadIndex);
                    job.MainThreadSynchronizer.SignalFinish();
                    break;
                }

                canContinue = job.RowDistributor.DistributeRows(job);
            }
        }

        /// <summary>
        /// Class serves as a job argument to a thread work.
        /// </summary>
        class ParallelMergeRowJob
        {
            /// <summary>
            /// Synchronizer to the main thread.
            /// </summary>
            public Synchronizer MainThreadSynchronizer;
            public RowDistributor RowDistributor;
            public MatchResultsStorage Elements;
            /// <summary>
            /// Index that describes the order of the merge work and who continues to the next round.
            /// </summary>
            public int ThreadIndex; 
            public int FirstRow;
            public int SecondRow;

            public ParallelMergeRowJob( int threadIndex, Synchronizer synchronizer, RowDistributor columnDistributor, MatchResultsStorage elements)
            {
                this.ThreadIndex = threadIndex;
                this.MainThreadSynchronizer = synchronizer;
                this.Elements = elements;
                this.RowDistributor = columnDistributor;
            }
        }

        /// <summary>
        /// A class serves as a row dsitributor.
        /// When a thread finished merging its column, it calls method for more work.
        /// </summary>
        class RowDistributor
        {
            object lockingObject = new object();
            int finishedThreads;
            /// <summary>
            /// Threads to be used in a round.
            /// </summary>
            int workThreads;
            /// <summary>
            /// Rows to merge.
            /// </summary>
            int rowCount;
            /// <summary>
            /// Reference to the jobs of threads.
            /// </summary>
            List<ParallelMergeRowJob> threadJobs = new List<ParallelMergeRowJob>();

            public RowDistributor(int workThreads, int rowCount)
            {
                this.rowCount = rowCount;
                this.workThreads = workThreads;
                this.finishedThreads = 0;
            }

            public void AddJob(ParallelMergeRowJob job)
            {
                this.threadJobs.Add(job);
            }

            /// <summary>
            /// A method that thread that merges row calls when it finishes the mergins.
            /// </summary>
            /// <param name="threadJob"> A job of a thread that call this method.</param>
            /// <returns> If the thread can continue to the next round.</returns>
            public bool DistributeRows(ParallelMergeRowJob threadJob)
            {
                    lock (this.lockingObject)
                    {
                        this.finishedThreads++;
                        bool IsInNextRound = ((this.workThreads / 2) < threadJob.ThreadIndex ? false : true);
                        
                        // Am I the last working thread?
                        if (this.finishedThreads != this.workThreads)
                        {   // No.
                            // Will I work in the next round?
                                  // No, then end completely.
                            if (!IsInNextRound) return false;
                            else  // Yes, wait for the last thread to finish.
                            {
                                Monitor.Wait(this.lockingObject);
                                return true;
                                // it assumed after they are woken up, the appropriate jobs were assigned.
                            }
                        } else
                        {   // Yes, I am last.
                            PrepareNextRound();
                            // Wake up all waiting threads.
                            Monitor.PulseAll(this.lockingObject);

                            if (!IsInNextRound || this.workThreads == 0) return false;
                            else return true;
                        }
                    }
            }

            /// <summary>
            /// Prepares for the next round.
            /// Number of threads is reduced by two and the row count is reduced by two as well.
            /// </summary>
            private void PrepareNextRound()
            {
                this.workThreads /= 2;
                this.finishedThreads = 0;
                this.rowCount /= 2;

                // Assign work to all threads that continue to the next round.
                for (int i = 0; i < this.workThreads; i++)
                {
                    this.threadJobs[i].FirstRow = i;

                    // if the number of row count is odd, the thread that receives touple (0, last) also merges (0, last-1)
                    // Other threads then receive values shifted by two instead of 1.
                    if ( i != 0 && this.rowCount % 2 == 1)
                         this.threadJobs[i].SecondRow = this.rowCount - i - 2;
                     else this.threadJobs[i].SecondRow = this.rowCount - i - 1;
                }
            }
        }



        #endregion MergeRow

        #endregion ParalelMerge



    }
}
