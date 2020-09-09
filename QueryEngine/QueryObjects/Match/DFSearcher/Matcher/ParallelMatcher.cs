/*! \file
This class includes definitions of dfs search parallel algorithm used to find pattern defined
in query match expression.
  
This paralel version only uses single threaded versions of the dfs search algorithm.
The one single threaded matcher should not be used alone because it was made to be used by the parallel.
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
using System.Runtime.CompilerServices;

namespace QueryEngine
{
    /// <summary>
    /// Serves as a paraller searcher. Contains threads and matchers.
    /// Class contains definitions of jobs for threads and vertex distributor.
    /// If only one thread is used for matching the single thread variant is used otherwise the multithread variant is used.
    /// </summary>
    internal sealed class DFSParallelPatternMatcher : IPatternMatcher
    {
         ISingleThreadMatcher[] Matchers;
         Graph Graph;
         IMatchExecutionHelper executionHelper;
         MatchResultsStorage Results;

        /// <summary>
        /// Creates a parallel matchers.
        /// Inits arrays of threads and matchers based on thread count.
        /// </summary>
        /// <param name="pattern"> Pattern to match. </param>
        /// <param name="graph"> Graph to search on.</param>
        /// <param name="results"> Where to store results. </param>
        /// <param name="executionHelper"> Query execution helper. </param>
        public DFSParallelPatternMatcher(IDFSPattern pattern, Graph graph, MatchResultsStorage results, IMatchExecutionHelper executionHelper)
        {
            if (executionHelper.ThreadCount <= 0 || executionHelper.VerticesPerThread <= 0)
                throw new ArgumentException($"{this.GetType()}, invalid number of threads or vertices per thread.");

            this.Matchers = new ISingleThreadMatcher[executionHelper.ThreadCount];
            this.Graph = graph;
            this.Results = results;
            this.executionHelper = executionHelper;

            for (int i = 0; i < executionHelper.ThreadCount; i++)
            {
                this.Matchers[i] = (ISingleThreadMatcher)MatchFactory
                                   .CreateMatcher("DFSSingleThread",                  // Type of Matcher 
                                                  i == 0 ? pattern : pattern.Clone(), // Cloning of pattern (one was already created)
                                                  graph,
                                                  results.GetThreadResults(i));
            }
        }

        /// <summary>
        /// Initiates search on graph.
        /// There are two possibilities.
        /// Based on number of threads. Either the main thread does the search it self,
        /// or the parallel search is used if more threads can be used.
        /// The Parallel search works in two steps, the first step is the graph search where each thread 
        /// saves its results into separate bins (however, still inside one result structure).
        /// When the matchers finish, the results are then parallelly merged into one bin.
        /// Notice the setting if the matchers should store the results directly is done here,
        /// because during parsing of the user input, the value can be changed later on. (For exmample seting order by).
        /// At the end, the counts of matched results are collected direcly, since sometimes user can input 
        /// only count(*) and using one shared count is time consuming.
        /// </summary>
        public void Search()
        {
            this.SetStoringResultsOnMatchers();
            QueryEngine.stopwatch.Start();

            if (!this.executionHelper.InParallel) 
            { 
                this.Matchers[0].Search();
                this.Results.IsMerged = true;
            }
            else
            {
                this.ParallelSearch();

                Console.WriteLine("Finished Search:");
                QueryEngine.PrintElapsedTime();

                if (this.executionHelper.IsMergeNeeded && this.executionHelper.IsStoringResult)
                {
                    this.ParallelMergeThreadResults();
                    this.Results.IsMerged = true;
                    Console.WriteLine("Finished Merge:");
                    QueryEngine.PrintElapsedTime();
                }
            }
            this.CollectCountFromMatchers();

        }

        /// <summary>
        /// Sets current value whether to store results of matchers.
        /// </summary>
        private void SetStoringResultsOnMatchers()
        {
            for (int i = 0; i < this.Matchers.Length; i++)
                this.Matchers[i].SetStoringResults(this.executionHelper.IsStoringResult);
        }

        /// <summary>
        /// Collects result count from each mather.
        /// This is done separately because sometimes the results are not stored in the result structure
        /// but only count is needed... such as "select count(*) match (x);).
        /// </summary>
        private void CollectCountFromMatchers()
        {
            for (int i = 0; i < this.Matchers.Length; i++)
                this.Results.NumberOfMatchedElements += this.Matchers[i].GetNumberOfMatchedElements();
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
            var distributor = new VertexDistributor(this.Graph.GetAllVertices(), this.executionHelper.VerticesPerThread);
            
            // -1 because the last index is ment for the main app thread.
            Task[] tasks = new Task[this.executionHelper.ThreadCount -1];
            // Create task for each matcher except the last mather and enqueue them into thread pool.
            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = new JobMultiThreadSearch(distributor, this.Matchers[i]);
                tasks[i] = Task.Factory.StartNew(() => DFSParallelPatternMatcher.WorkMultiThreadSearch(tmp));
            }

            // The last matcher is used by the main app thread.
            DFSParallelPatternMatcher.WorkMultiThreadSearch(new JobMultiThreadSearch(distributor, this.Matchers[this.executionHelper.ThreadCount - 1]));
            
            Task.WaitAll(tasks);
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
                if (start == -1 || end == -1) break;
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

            public JobMultiThreadSearch(VertexDistributor vertexDistributor, ISingleThreadMatcher matcher)
            {
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
            readonly List<Vertex> Vertices;
            /// <summary>
            /// Number of vertices to give during vertex distribution method call.
            /// </summary>
            readonly int VerticesPerRound;
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

 
        #region ParalelMerge

        /// <summary>
        /// Update: So far, on desktop machine, the fastest way was using merging with columns, even
        /// with only three columns and 16 threads (rows in the table). Actually, it is 
        /// much more memory efficient as well. But needs to be tried on a bigger computer, probably
        /// with a much faster ram.
        /// Merges results from all threads into one list. Note that the number of rows to merge is the same number
        /// as the number of threads (so one row represents one thread from the search algorithm).
        /// Merging can be done in two ways. If the half of available threads is larger than the number of columns the merge rows method
        /// is used because the number of threads for merging columns would be smaller, if the thread number lowers below the column count
        /// it skips to merging by column. In every other case, the merging by column is used.
        /// The first method: MergeRows, merges rows in parallel, it recursively splits range of rows into two parts while the first range
        /// is being merged with the one thread while the other range is being merged with a second thread.
        /// The second method: MergeColumn does parallel column merging, instead of merging of rows, it assignes columns to threads that merge the entire
        /// columns into one column.
        /// </summary>
        private void ParallelMergeThreadResults()
        {
          //   if (this.ThreadCount / 2 > this.Results.ColumnCount)
          //    MergeRows();
          //   else
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
            var mergeColumnJob = new ParallelMergeColumnJob(columnDistributor, this.Results);

            int threadsToUse = (this.executionHelper.ThreadCount < this.Results.ColumnCount ?
                                this.executionHelper.ThreadCount : this.Results.ColumnCount);
            
            // -1 because the main app thread will work as well.
            Task[] tasks = new Task[threadsToUse - 1];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Task.Factory.StartNew(() => DFSParallelPatternMatcher.ParallelMergeColumnWork(mergeColumnJob));

            DFSParallelPatternMatcher.ParallelMergeColumnWork(mergeColumnJob);
            Task.WaitAll(tasks);
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
                
                if (columnIndex == -1) break;
                else job.Elements.MergeColumn(columnIndex);
            }
        }

        /// <summary>
        /// Passes as an argument to a paralel merge work.
        /// </summary>
        private class ParallelMergeColumnJob
        {
            public MatchResultsStorage Elements;
            public ColumnDistributor ColumnDistributor;
            public ParallelMergeColumnJob(ColumnDistributor columnDistributor, MatchResultsStorage elements)
            {
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
            readonly int columnCount; 

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

        #region MergeRows

        /// <summary>
        /// The merge can be view as a binary tree. Where merging is done bottom up.
        /// If the number of used threads for merging on a tree level is smaller than the number of columns, the algorithm
        /// will skip into merging by column because it can use more threads this way, because each level lowers the number of threads by half.
        /// </summary>
        private void MergeRows()
        {
           DFSParallelPatternMatcher.ParallelMergeRowWork(this.Results, 0, this.executionHelper.ThreadCount, 1);
           if (this.Results.ColumnCount != 1)
                this.MergeColumn();
        }

        /// <summary>
        /// A merge that looks like a binary tree. On each level of the binary tree, the thread knows what number of threads is used
        /// to merge results from the lower level. If the number of used threads is smaller than the number of columns in the result table, the algorithm
        /// will skip into merging by column instead of continuing with row merging. Why? Because more threads can be used for work.
        /// Each call, the method receives a range to merge. If the range is smaller or equals three, it merges the results into the
        /// row on position of the start argument. 
        /// If the range is larger, the work can be split into two threads by calculating a middle position, then two threads receive
        /// the halves of the range, until the range become small enough to be merged. (Binary tree build up.)
        /// If the middle is on an odd number, the middle is moved one position back. What this means is that if we want to split range of 10,
        /// middle is 5, and ranges 0-5, 5-10 are worked on separately, it would continue into 0 2, 2 5, 5 7, 7 9 merges. Which ommits one possible merge.
        /// Each merge is stored to the position of start argument.
        /// </summary>
        /// <param name="results"> Result table to merge.</param>
        /// <param name="start"> Starting index of the range of rows to merge. </param>
        /// <param name="end"> End index of the range of rows to merge. </param>
        /// <param name="threadsOnLevel"> A number of threads used for mergin on a current tree level. </param>
        private static void ParallelMergeRowWork(MatchResultsStorage results, int start, int end, int threadsOnLevel)
        {
            if (end - start > 3)
            {
                // compute middle of the range
                int middle = ((end - start) / 2) + start;
                if (middle % 2 == 1) middle--;

                // Spawned merge thread.
                Task task = Task.Factory.StartNew(() => DFSParallelPatternMatcher.ParallelMergeRowWork(results, middle, end, threadsOnLevel * 2));
                // Current thread work.
                DFSParallelPatternMatcher.ParallelMergeRowWork(results, start, middle, threadsOnLevel*2);
                
                // Wait for the other task to finish and start merging its results with yours.
                task.Wait();
                if (threadsOnLevel >= results.ColumnCount)
                    results.MergeRows(start, middle);
                else return;  /* do nothing */

            } else
            { // Merge rows
                for (int i = start + 1; i < end; i++)
                    results.MergeRows(start, i);
            }
        }


        #endregion MergeRow

        #endregion ParalelMerge
   

    }
}
