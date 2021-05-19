/*! \file
This file contains a definition of a execution helper.
The execution helper's job is to help with the execution of specific clauses of during query computation.
That means, it contains information that defines execution of the clauses. Such as a thread count that is used
to create appropriate number of Matcher inside MatchObject, or pick the correct implementation of processing algorithms.
Each query object adds interface to the helper that the object needs.
 */

namespace QueryEngine
{
 
    /// <summary>
    /// A base interface for every execution helper extension.
    /// Must be visible to all query objects.
    /// </summary>
    internal interface IBaseExecutionHelper
    {
        /// <summary>
        /// Defines whether an optional clause order by was defined in the user input query.
        /// </summary>
        bool IsSetOrderBy { get; set; }

        /// <summary>
        /// Defines whether an optional clause group by was defined in the user input query.
        /// </summary>
        bool IsSetGroupBy { get; set; }

        bool IsSetSingleGroupGroupBy { get; set; }

        /// <summary>
        /// A number of threads that will be used during query execution.
        /// </summary>
        int ThreadCount { get; }

        /// <summary>
        /// If more than one thread must be used return true, otherwise false.
        /// </summary>
        bool InParallel { get; }

        bool IsStoringResult { get; set; }
        /// <summary>
        /// A size of blocks to store matcher results.
        /// </summary>
        int FixedArraySize { get; }

    }

    internal interface IMatchExecutionHelper : IBaseExecutionHelper
    {
        /// <summary>
        /// If more than one thread is used to search,
        /// this defines a number of vertices that will be distributed to threads during matching algorithm.
        /// </summary>
        int VerticesPerThread { get; }

        /// <summary>
        /// A name of used parallel pattern matcher.
        /// </summary>
        string  ParallelPatternMatcherName { get; }

        /// <summary>
        /// A name of used single thread pattern matcher used by the parallel pattern matcher.
        /// </summary>
        string  SingleThreadPatternMatcherName { get; }
        /// <summary>
        /// A name of used pattern.
        /// </summary>
        string PatternName { get; }
    }

    internal interface ISelectExecutionHelper : IBaseExecutionHelper
    {
        /// <summary>
        /// A type of a printer for printing results.
        /// Used inside print method for factory method of printer.
        /// </summary>
        PrinterType Printer { get; }
        /// <summary>
        /// A type of the printing format.
        /// </summary>
        FormaterType Formater { get; }

        /// <summary>
        /// A file name where to print results.
        /// </summary>
        string FileName { get; }

    }

    internal interface IOrderByExecutionHelper : IBaseExecutionHelper
    { 
        SorterAlias SorterAlias { get; }
    }

    internal interface IGroupByExecutionHelper : IBaseExecutionHelper 
    { 
        GrouperAlias GrouperAlias { get; }
    }

    /// <summary>
    /// The query passes this execution helper to its query objects and each object sees only the neccessary information for its own execution.
    /// </summary>
    internal class QueryExecutionHelper : IMatchExecutionHelper, ISelectExecutionHelper, IOrderByExecutionHelper, IGroupByExecutionHelper
    {
        public QueryExecutionHelper(int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, int arraySize, string fileName, string ppmName, string stpmName, string patternName, GrouperAlias grouperName, SorterAlias sorterName)
        {
            this.ThreadCount = threadCount;
            this.Printer = printer;
            this.Formater = formater;
            this.VerticesPerThread = verticesPerThread;
            this.FixedArraySize = arraySize;
            this.FileName = fileName;
            this.ParallelPatternMatcherName = ppmName;
            this.SingleThreadPatternMatcherName = stpmName;
            this.PatternName = patternName;
            this.GrouperAlias = grouperName;
            this.SorterAlias = sorterName;
        }

        public int ThreadCount { get; }
        public bool IsSetOrderBy { get; set; } = false;
        public bool IsSetGroupBy { get; set; } = false;
        public bool IsSetSingleGroupGroupBy { get; set; } = false;

        public int VerticesPerThread { get; }
        public int FixedArraySize { get; }
        
        public PrinterType Printer {get; }
        public FormaterType Formater {get; }
        public string FileName {get; }
        
        public bool IsStoringResult { get; set; } = true;
        public bool InParallel => ThreadCount != 1;

        public string ParallelPatternMatcherName { get; }
        public string SingleThreadPatternMatcherName { get; }
        public string PatternName { get; }

        public GrouperAlias GrouperAlias { get; }
        public SorterAlias SorterAlias { get;  }

    }
}
