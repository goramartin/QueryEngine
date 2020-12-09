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
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    /// <summary>
    /// Serves as a paraller searcher. Contains threads and matchers.
    /// Class contains definitions of jobs for threads and vertex distributor.
    /// If only one thread is used for matching the single thread variant is used otherwise the multithread variant is used.
    /// </summary>
    internal sealed class DFSParallelPatternMatcher : IPatternMatcher
    {
        private ISingleThreadPatternMatcher[] matchers;
        private Graph graph;
        private IMatchExecutionHelper executionHelper;
        private MatchResultsStorage results;

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

            this.matchers = new ISingleThreadPatternMatcher[executionHelper.ThreadCount];
            this.graph = graph;
            this.results = results;
            this.executionHelper = executionHelper;

            for (int i = 0; i < executionHelper.ThreadCount; i++)
            {
                this.matchers[i] = (ISingleThreadPatternMatcher)MatchFactory
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
                this.matchers[0].Search();
                this.results.IsMerged = true;
            }
            else
            {
                this.ParallelSearch();

                Console.WriteLine("Finished Search:");
                QueryEngine.PrintElapsedTime();

                if (this.executionHelper.IsMergeNeeded)
                {
                    this.ParallelMergeThreadResults();
                    this.results.IsMerged = true;
                    Console.WriteLine("Finished Merge:");
                    QueryEngine.PrintElapsedTime();
                }
            }
            this.CollectCountFromMatchers();

            Console.WriteLine("Finished Search:");
            QueryEngine.PrintElapsedTime();

        }

        /// <summary>
        /// Sets current value whether to store results of matchers.
        /// </summary>
        private void SetStoringResultsOnMatchers()
        {
            for (int i = 0; i < this.matchers.Length; i++)
                this.matchers[i].SetStoringResults(this.executionHelper.IsStoringResult);
        }

        /// <summary>
        /// Collects result count from each mather.
        /// This is done separately because sometimes the results are not stored in the result structure
        /// but only count is needed... such as "select count(*) match (x);).
        /// </summary>
        private void CollectCountFromMatchers()
        {
            for (int i = 0; i < this.matchers.Length; i++)
                this.results.NumberOfMatchedElements += this.matchers[i].GetNumberOfMatchedElements();
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
            var distributor = new VertexDistributor(this.graph.GetAllVertices(), this.executionHelper.VerticesPerThread);
            
            // -1 because the last index is ment for the main app thread.
            Task[] tasks = new Task[this.executionHelper.ThreadCount -1];
            // Create task for each matcher except the last mather and enqueue them into thread pool.
            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = new JobMultiThreadSearch(distributor, this.matchers[i]);
                tasks[i] = Task.Factory.StartNew(() => DFSParallelPatternMatcher.WorkMultiThreadSearch(tmp));
            }

            // The last matcher is used by the main app thread.
            DFSParallelPatternMatcher.WorkMultiThreadSearch(new JobMultiThreadSearch(distributor, this.matchers[this.executionHelper.ThreadCount - 1]));
            
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
                job.distributor.DistributeVertices(ref start, ref end);

                // No more vertices. The thread can end.
                if (start == -1 || end == -1) break;
                else
                {
                    // Set the range of vertices to the matcher and start searching the graph.
                    job.matcher.SetStartingVerticesIndeces(start, end);
                    job.matcher.Search();
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
            public VertexDistributor distributor;
            public ISingleThreadPatternMatcher matcher;

            public JobMultiThreadSearch(VertexDistributor vertexDistributor, ISingleThreadPatternMatcher matcher)
            {
                this.distributor = vertexDistributor;
                this.matcher = matcher;
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
            var columnDistributor = new ColumnDistributor(this.results.ColumnCount);
            var mergeColumnJob = new ParallelMergeColumnJob(columnDistributor, this.results);

            int threadsToUse = (this.executionHelper.ThreadCount < this.results.ColumnCount ?
                                this.executionHelper.ThreadCount : this.results.ColumnCount);
            
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
                columnIndex = job.columnDistributor.DistributeColumn();
                
                if (columnIndex == -1) break;
                else job.elements.MergeColumn(columnIndex);
            }
        }

        /// <summary>
        /// Passes as an argument to a paralel merge work.
        /// </summary>
        private class ParallelMergeColumnJob
        {
            public MatchResultsStorage elements;
            public ColumnDistributor columnDistributor;
            public ParallelMergeColumnJob(ColumnDistributor columnDistributor, MatchResultsStorage elements)
            {
                this.elements = elements;
                this.columnDistributor = columnDistributor;
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
           DFSParallelPatternMatcher.ParallelMergeRowWork(this.results, 0, this.executionHelper.ThreadCount, 1);
           if (this.results.ColumnCount != 1)
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
