/*! \file
This class includes definitions of dfs search parallel algorithm used to find pattern defined
in query match expression.
  
This paralel version only uses single threaded versions of the dfs search algorithm.
The one single threaded matcher should not be used alone because it was made to be used by the parallel.
The parallel algorithm is lock-free algorithm, saving results have been made lock free thanks to 
storing result into their own place inside query result structure.
Also, division of work is done lock free thanks to interlocked class that allows to devide workload of the threads
with the help of VertexDistributor and ColumnDistributor.

This version of the matcher uses a class MatchInternalFixedResults for storing the results. 
For more info visit a file named MatchInternalFixedResults.cs. 
 */

using System;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Serves as a paraller searcher. Contains threads and matchers.
    /// Class contains definitions of jobs for threads.
    /// If only one thread is used for matching the single thread variant is used otherwise the multithread variant is used.
    /// If the parallel version is used, the results are merged into one table. at the end of the search.
    /// </summary>
    internal sealed class DFSParallelPatternMatcher : DFSParallelPatternMatcherBase
    {
        private MatchFixedResults results;
        private ISingleThreadPatternMatcher[] matchers;

        /// <summary>
        /// Creates a parallel matchers.
        /// Inits arrays of threads and matchers based on thread count.
        /// </summary>
        /// <param name="pattern"> Pattern to match. </param>
        /// <param name="graph"> Graph to search on.</param>
        /// <param name="results"> Where to store results. </param>
        /// <param name="executionHelper"> Query execution helper. </param>
        public DFSParallelPatternMatcher(DFSPattern pattern, Graph graph, MatchFixedResults results, IMatchExecutionHelper executionHelper): base(graph, executionHelper)
        {
            if (pattern == null || results == null)
                throw new ArgumentNullException($"{this.GetType()}, passed a null to a construtor.");
            
            this.matchers = new ISingleThreadPatternMatcher[this.helper.ThreadCount];
            this.results = results;
            for (int i = 0; i < this.helper.ThreadCount; i++)
            {
                this.matchers[i] = (ISingleThreadPatternMatcher)MatchFactory
                                   .CreateMatcher(this.helper.SingleThreadPatternMatcherName,                  // Type of Matcher 
                                                  i == 0 ? pattern : pattern.Clone(), // Cloning of pattern (one was already created)
                                                  graph,
                                                  results.GetMatcherResultsStorage(i)); // Result storage
            }
        }

        /// <summary>
        /// Initiates search on graph.
        /// There are two possibilities.
        /// Based on number of threads. Either the main thread does the search it self,
        /// or the parallel search is used if more threads can be used.
        /// The Parallel search works in two steps, the first step is the graph search where each thread 
        /// saves its results into separate bins (however, still inside one result structure).
        /// When the matchers finish, the results are then paralelly merged into one structure. (If it runs in single-thread,
        /// the merging functions are called from within one thread and it merges all columns which in this case means
        /// only move storage blocks into List.)
        /// Notice that the setting whether the matcher should store results is done here.
        /// It is because, during parsing of the user input, the value can be changed later on. (For exmample seting order by).
        /// At the end, the counts of matched results are collected directly from each mather, since sometimes user can input 
        /// only count(*) and using one shared count with atomics is worse.
        /// </summary>
        public override void Search()
        {
            this.SetStoringResults(this.helper.IsStoringResult);
            QueryEngine.stopwatch.Start();

            if (!this.helper.InParallel) 
            { 
                this.matchers[0].Search();
                
                Console.WriteLine("Finished Search:");
                QueryEngine.PrintElapsedTime();
                
                this.results.MergeAllColumns();
            }
            else
            {
                this.ParallelSearch();

                Console.WriteLine("Finished Search:");
                QueryEngine.PrintElapsedTime();

                if (this.helper.IsStoringResult)
                    this.ParallelMergeThreadResults();
            }
            this.CollectCountFromMatchers();

            Console.WriteLine("Finished Search Complete:");
            QueryEngine.PrintElapsedTime();

        }

        public override void SetStoringResults(bool storeResults)
        {
            for (int i = 0; i < this.matchers.Length; i++)
                this.matchers[i].SetStoringResults(storeResults);
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
            var distributor = new VertexDistributor(this.graph.GetAllVertices(), this.helper.VerticesPerThread);
            
            // -1 because the last index is ment for the main app thread.
            Task[] tasks = new Task[this.helper.ThreadCount -1];
            // Create task for each matcher except the last mather and enqueue them into thread pool.
            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = new JobMultiThreadSearch(distributor, this.matchers[i]);
                tasks[i] = Task.Factory.StartNew(() => WorkMultiThreadSearch(tmp));
            }

            // The last matcher is used by the main app thread.
            WorkMultiThreadSearch(new JobMultiThreadSearch(distributor, this.matchers[this.helper.ThreadCount - 1]));
            
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
        /// Merges columns in parallel. Each thread is given a column. The matchers stored results in their local result tables.
        /// The thread merges the same column from all the result tables into a one column. More about the algorithm is in the match internal results
        /// folder.
        /// </summary>
        private void ParallelMergeThreadResults()
        {
             MergeByColumn();
        }

        #region MergeByColumn

        /// <summary>
        /// Merges columns of the result table in parallel.
        /// Each thread askes for a column to merge from a column distributor.
        /// If there are no more columns to merge, the thread finishes.
        /// </summary>
        private void MergeByColumn()
        {
            var columnDistributor = new ColumnDistributor(this.results.ColumnCount);
            var mergeColumnJob = new ParallelMergeColumnJob(columnDistributor, this.results);

            int threadsToUse = (this.helper.ThreadCount < this.results.ColumnCount ?
                                this.helper.ThreadCount : this.results.ColumnCount);
            
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
                else job.matcherResults.MergeColumn(columnIndex);
            }
        }

        /// <summary>
        /// Passes as an argument to a paralel merge work.
        /// </summary>
        private class ParallelMergeColumnJob
        {
            public MatchFixedResults matcherResults;
            public ColumnDistributor columnDistributor;
            public ParallelMergeColumnJob(ColumnDistributor columnDistributor, MatchFixedResults matcherResults)
            {
                this.matcherResults = matcherResults;
                this.columnDistributor = columnDistributor;
            }
        }

        #endregion MergeByColumn

        #endregion ParalelMerge


    }
}
