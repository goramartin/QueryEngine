using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// The class DFSParallelPatternMatcher was split to provide a better foundation for 
    /// streamed version of the parallel matcher.
    /// </summary>
    internal abstract class DFSParallelPatternMatcherBase : IPatternMatcher
    {
        protected Graph graph;
        protected IMatchExecutionHelper helper;

        protected DFSParallelPatternMatcherBase(Graph graph, IMatchExecutionHelper helper)
        {
            if (graph == null || helper == null)
                throw new ArgumentNullException($"{this.GetType()}, was passsed a null to a constructor.");
            else if (helper.ThreadCount <= 0 || helper.VerticesPerThread <= 0)
                throw new ArgumentException($"{this.GetType()}, invalid number of threads or vertices per thread.");
            else
            {
                this.graph = graph;
                this.helper = helper;
            }
        }

        public abstract void Search();
        public abstract void SetStoringResults(bool storeResults);
    }
}
