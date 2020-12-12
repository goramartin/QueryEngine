using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// The class will perform a parallel search on the graph using single threaded versions
    /// of the pattern matchers.
    /// It uses the VertexDistributor as in DFSParallelPatternMatcher.
    /// The only difference between this class and DFSParallelPatternMatcher is that this class
    /// will not merge results and it can pass resultProcessor to the matchers.
    /// </summary>
    internal class DFSParallelPatternMatcherStreamed : DFSParallelPatternMatcherBase, IPatternMatcherStreamed
    {
        private ISingleThreadPatternMatcherStreamed[] matchers;
        /// <summary>
        /// Is set in the method PassResultProcessor.
        /// </summary>
        private ResultProcessor resultProcessor;

        /// <summary>
        /// Creates a parallel matchers.
        /// Inits arrays of threads and matchers based on thread count.
        /// </summary>
        /// <param name="pattern"> Pattern to match. </param>
        /// <param name="graph"> Graph to search on.</param>
        /// <param name="executionHelper"> Query execution helper. </param>
        public DFSParallelPatternMatcherStreamed(DFSPattern pattern, Graph graph, IMatchExecutionHelper executionHelper) : base(graph, executionHelper)
        {
            this.matchers = new ISingleThreadPatternMatcherStreamed[this.helper.ThreadCount];
            for (int i = 0; i < this.helper.ThreadCount; i++)
            {
                this.matchers[i] = (ISingleThreadPatternMatcherStreamed)MatchFactory
                                   .CreateMatcher(this.helper.SingleThreadPatternMatcherName, // Type of Matcher 
                                                  i == 0 ? pattern : pattern.Clone(), // Cloning of pattern (one was already created)
                                                  graph,
                                                  i); // Matcher ID
            }
        }

        public override void Search()
        {
            this.SetStoringResults(this.helper.IsStoringResult);

            QueryEngine.stopwatch.Start();

            if (!this.helper.InParallel)
            {
                this.matchers[0].Search();
                // Notify the processor that this matcher ended.
                this.resultProcessor.Process(0, null);
            }
            else this.ParallelSearch();

            Console.WriteLine("Finished Search:");
            QueryEngine.PrintElapsedTime();
        } 

        public void PassResultProcessor(ResultProcessor resultProcessor)
        {
            this.resultProcessor = resultProcessor;
            for (int i = 0; i < this.matchers.Length; i++)
                this.matchers[i].PassResultProcessor(resultProcessor);
        }

        public override void SetStoringResults(bool storeResults)
        {
            for (int i = 0; i < this.matchers.Length; i++)
                this.matchers[i].SetStoringResults(storeResults);
        }

        private void ParallelSearch()
        {
            var distributor = new VertexDistributor(this.graph.GetAllVertices(), this.helper.VerticesPerThread);

            // -1 because the last index is ment for the main app thread.
            Task[] tasks = new Task[this.helper.ThreadCount - 1];
            // Create task for each matcher except the last mather and enqueue them into thread pool.
            for (int i = 0; i < tasks.Length; i++)
            {
                var tmp = new JobMultiThreadSearch(distributor, this.matchers[i], i, this.resultProcessor);
                tasks[i] = Task.Factory.StartNew(() => WorkMultiThreadSearch(tmp));
            }

            // The last matcher is used by the main app thread.
            WorkMultiThreadSearch(new JobMultiThreadSearch(distributor, this.matchers[this.helper.ThreadCount - 1], this.helper.ThreadCount - 1, this.resultProcessor));

            Task.WaitAll(tasks);
        }


        /// <summary>
        /// Method passed to threads.
        /// A thread asks for a new starting vertices for his matcher from a vertex distributor.
        /// If there are no more vertices the method ends and notified the processor that the matcher ended search.
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
                if (start == -1 || end == -1)
                {
                    // Notify the processor that this matcher ended.
                    job.resultProcessor.Process(job.matcherID, null);
                    break;
                }
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
            public ResultProcessor resultProcessor;
            public ISingleThreadPatternMatcherStreamed matcher;
            public int matcherID;

            public JobMultiThreadSearch(VertexDistributor vertexDistributor, ISingleThreadPatternMatcherStreamed matcher, int mID, ResultProcessor resProc)
            {
                this.distributor = vertexDistributor;
                this.matcher = matcher;
                this.matcherID = mID;
                this.resultProcessor = resProc;
            }
        }
    }
}
