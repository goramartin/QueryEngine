/*! \file
This file includes definition of one query and the streamed query version as well.
It is the same class, only different constructors are used. 

Normal Query:
This could be considered as a facade because it includes classes that form more complicated 
structures.
Query is formed by query objects, those are match object, select object, order by ...
Those objects represent information parsed from the inputted query.
They also perform the duties with relation to their semantic meaning, such as, match object conducts matching on the graph,
select prints results to the output and orderby sorts the results.

The query is given a graph to compute the query on, a reader that reads user input query, a query execution helper that provides neccessary information
for the query objects (for example the number of threads to use).

The query itself is constructed as follows.
Firstly the user input is tokenized from the reader. Then parsed trees are created from the tokens.
Parsed trees are passed to constructors of each query object and then visitors collect all the neccessary information.

The query objects inside the Query class form a simple execution plan, where the objects that has to finish first are put at the end of the chains and vice versa.
The user calls compute on the query and the chains calls recursively to the last object. When object finished, it passes arguments in 
the out parameter and the computation continues.

Some query clauses must be present every time. Those are the select and the match clause.
Other clauses are purely optional. Those are group by and order by.

Example: 
Clauses Select, Match, Order by form a chain Select -> Order by -> Match. After the Match is finished, it passes its results to the Order by.

Streamed Query:
The only difference is that instead of chaining the objects that has to finish first to the end, they are the first one in the chains.
The classes for this type of chaining are called ResultProcessors. Note that the SelectObject and MatchObject are the same as before.

Thus, the chains is alwasy Select -> Match and now instead of chaining the OrderBy or GroupBy in between the Select and Match. The processors
are chained in such a way, that the Matchers of the MatchObject store direct references to them. This enables to pass the brand new results of the 
matcher for further processing immediatly upon its find.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace QueryEngine
{
     public enum QueryMode { Normal, Streamed, HalfStreamed };
    /// <summary>
    /// Represents a pgql query.
    /// It tokenizes, parses and constructs simple execution chain.
    /// Provides api to execute the query.
    /// </summary>
    public sealed class Query
    {
        private Graph graph;
        private VariableMap variableMap;
        /// <summary>
        /// An execution chain.
        /// </summary>
        private QueryObject query;
        private QueryExecutionHelper qEhelper;
        private QueryExpressionInfo exprInfo; 
        public bool Finished { get; private set; }

        /// <summary>
        /// Builds a query from an input.
        /// Cannot contain order by and group by simultaneously.
        /// Called by static Create method.
        /// </summary>
        /// <param name="tokens"> An input query. </param>
        /// <param name="graph"> A graph to compute the query on. </param>
        /// <param name="threadCount"> Maximum number of threads available to use. </param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName"> A file to store results into if set, otherwise null. </param>
        /// <param name="allowPrint"> Whether to allow printing. Mainly for benchmarking purposes.</param>
        /// <param name="fixedArraySize"> The size of the arrays used for storing results of the matcher.</param>
        /// <param name="grouperAlias"> A grouper to use when specifying group by.</param>
        /// <param name="sorterAlias"> A sorter to use when specifying order by.</param>
        private Query(List<Token> tokens, Graph graph, bool allowPrint, int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, string fileName, GrouperAlias grouperAlias, SorterAlias sorterAlias, int fixedArraySize)
        {
            this.graph = graph;
            this.variableMap = new VariableMap(); 
            this.qEhelper = new QueryExecutionHelper(threadCount, printer, formater, verticesPerThread, fixedArraySize, fileName, "DFSParallel", "DFSSingleThread", "SIMPLE", grouperAlias, sorterAlias);

            // Parse input query.
            var parsedClauses = Parser.Parse(tokens);

            // Create execution chain. 
            if (parsedClauses.ContainsKey(Parser.Clause.GroupBy) && parsedClauses.ContainsKey(Parser.Clause.OrderBy))
                throw new ArgumentException($"{this.GetType()}, the query cannot contain both order by and group by");
            QueryObject groupBy = null;
            QueryObject orderBy = null;
            
            // MATCH is always leaf.
            QueryObject match = QueryObject.Factory
                (typeof(MatchObject), graph, qEhelper, variableMap, parsedClauses[Parser.Clause.Match], null);

            // Second must be group by because it defines what can be in other clauses.
            // GROUP BY
            if (parsedClauses.ContainsKey(Parser.Clause.GroupBy))
            {
                this.exprInfo = new QueryExpressionInfo(true);
                groupBy = QueryObject.Factory(typeof(GroupByObject), graph, qEhelper, variableMap, parsedClauses[Parser.Clause.GroupBy], exprInfo);
            }
            else this.exprInfo = new QueryExpressionInfo(false);

            // SELECT is the last one to process the resuls.
            this.query = QueryObject.Factory
                (typeof(SelectObject), graph, qEhelper, variableMap, parsedClauses[Parser.Clause.Select], exprInfo);
            ((SelectObject)this.query).allowPrint = allowPrint;

            // Check if the results are in a single group.
            this.SetSingleGroupFlags();

            // ORDER BY
            if (parsedClauses.ContainsKey(Parser.Clause.OrderBy))
            {
                orderBy = QueryObject.Factory(typeof(OrderByObject), graph, qEhelper, variableMap, parsedClauses[Parser.Clause.OrderBy], exprInfo);
                query.AddToEnd(orderBy);
            }

            // If the single group by is set, add GroupBy object to the execution chain.
            if (this.qEhelper.IsSetSingleGroupGroupBy && !this.qEhelper.IsSetGroupBy)
                groupBy = QueryObject.Factory(typeof(GroupByObject), null, qEhelper, null, null, exprInfo);

            if (groupBy != null) query.AddToEnd(groupBy);
            query.AddToEnd(match);
            query.PassStoringVariables(this.exprInfo.CollectUsedVariables());
        }

        /// <summary>
        /// Builds a streamed version of a query from an input.
        /// Cannot contain order by and group by simultaneously.
        /// Called by static create method.
        /// </summary>
        /// <param name="tokens"> An input query.</param>
        /// <param name="graph"> A graph to compute the query on.</param>
        /// <param name="threadCount"> Maximum number of threads available to use.</param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName">  A file to store results into if set, otherwise null. </param>
        /// <param name="isStreamed"> A flag to distinguish a normal construtor from streamed constructor.</param>
        /// <param name="allowPrint"> Whether to allow printing. Mainly for benchmarking purposes.</param>
        /// <param name="fixedArraySize"> The size of the arrays used for storing results of the matcher.</param>
        /// <param name="grouperAlias"> A grouper to use when specifying group by.</param>
        /// <param name="sorterAlias"> A sorter to use when specifying order by.</param>
        private Query(List<Token> tokens, Graph graph, bool allowPrint, int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, string fileName, GrouperAlias grouperAlias, SorterAlias sorterAlias, int fixedArraySize, bool isStreamed)
        {
            this.graph = graph;
            this.variableMap = new VariableMap();
            this.qEhelper = new QueryExecutionHelper(threadCount, printer, formater, verticesPerThread, fixedArraySize, fileName, "DFSParallelStreamed", "DFSSingleThreadStreamed", "SIMPLE", grouperAlias, sorterAlias);

            // Parse input query.
            var parsedClauses = Parser.Parse(tokens);
            if (parsedClauses.ContainsKey(Parser.Clause.OrderBy) && parsedClauses.ContainsKey(Parser.Clause.GroupBy))
                throw new ArgumentException($"{this.GetType()}, the streamed version of the query cannot contain group by and order by at the same time.");

            // MATCH is always leaf.
            MatchObjectStreamed match = (MatchObjectStreamed)QueryObject.Factory
                 (typeof(MatchObjectStreamed), graph, qEhelper, variableMap, parsedClauses[Parser.Clause.Match], null);

            // GROUP BY and obtain the aggregates and hashes -> the all necessary info is in the expressionInfo class. 
            if (parsedClauses.ContainsKey(Parser.Clause.GroupBy))
            {
                this.exprInfo = new QueryExpressionInfo(true);
                GroupResultProcessor.ParseGroupBy(graph, variableMap, qEhelper, (GroupByNode)parsedClauses[Parser.Clause.GroupBy], exprInfo);
            }
            else this.exprInfo = new QueryExpressionInfo(false);

            // SELECT is the last one to process the resuls.
            this.query = QueryObject.Factory
                (typeof(SelectObject), graph, qEhelper, variableMap, parsedClauses[Parser.Clause.Select], exprInfo);
            ((SelectObject)this.query).allowPrint = allowPrint;

            SetSingleGroupFlags();

            // ORDER BY
            if (parsedClauses.ContainsKey(Parser.Clause.OrderBy))
            { 
                var comps = OrderByResultProcessor.ParseOrderBy(graph, variableMap, qEhelper, (OrderByNode)parsedClauses[Parser.Clause.OrderBy], exprInfo, variableMap.GetCount());
                var orderByProc = OrderByResultProcessor.Factory(this.exprInfo, comps, qEhelper,variableMap.GetCount(), this.exprInfo.CollectUsedVariables());
                match.PassResultProcessor(orderByProc);
            } else
            {
                // Check if the query is aggregation and not a simple query.
                if ((this.exprInfo.Aggregates.Count == 0 && this.qEhelper.IsSetSingleGroupGroupBy) || (!this.qEhelper.IsSetSingleGroupGroupBy && !parsedClauses.ContainsKey(Parser.Clause.GroupBy)))
                throw new ArgumentException($"{this.GetType()}, no grouping was specified. The group by streamed version allows to compute only aggregations.");
                var groupByProc = GroupResultProcessor.Factory(exprInfo, qEhelper, variableMap.GetCount(), this.exprInfo.CollectUsedVariables(), isStreamed);
                
                match.PassResultProcessor(groupByProc);
            }
            query.AddToEnd(match);
        }

        public void Compute()
        {
            if (!this.Finished)
            {
                this.query.Compute(out ITableResults resTable, out GroupByResults groupByResults);
                this.Finished = true;
            }
            else throw new Exception($"{this.GetType()}, trying to call a query that has already finished.");
        }

        /// <summary>
        /// Sets flags to execution helper after the definition of group by and select.
        /// The flag should be set to false in case there is not set group by and the select clause
        /// contains only count(*) because in that case the results are not needed. Otherwise they are needed.
        /// Note that this sets to false even though the order by is set.
        /// </summary>
        private void SetSingleGroupFlags()
        {
            // No group by
            if (!this.qEhelper.IsSetGroupBy)
            {
                // There are only aggregates
                if (this.exprInfo.Aggregates.Count > 0)
                {
                    bool foundNonAst = false;
                    for (int i = 0; i < this.exprInfo.Aggregates.Count; i++)
                        if (!this.exprInfo.Aggregates[i].IsAstCount) foundNonAst = true;
                    
                    // If there are only count(*) the results are not needed.
                    if (!foundNonAst ) this.qEhelper.IsStoringResult = false;
                    this.qEhelper.IsSetSingleGroupGroupBy = true;
                }
            }
        }

        public static Query Create(QueryMode mode, string inputQuery, Graph graph, int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, string fileName, GrouperAlias grouperAlias, SorterAlias sorterAlias, int fixedArraySize, bool allowPrint)
        {
            return CreateInternalL(mode, Tokenizer.Tokenize(inputQuery), graph, threadCount, printer, formater, verticesPerThread, fileName, grouperAlias, sorterAlias, fixedArraySize, allowPrint);
        }

        public static Query Create(QueryMode mode, TextReader inputQuery, Graph graph, int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, string fileName, GrouperAlias grouperAlias, SorterAlias sorterAlias, int fixedArraySize, bool allowPrint)
        {
            return CreateInternalL(mode, Tokenizer.Tokenize(inputQuery), graph, threadCount, printer, formater, verticesPerThread, fileName, grouperAlias, sorterAlias, fixedArraySize, allowPrint);
        }

        private static Query CreateInternalL(QueryMode mode, List<Token> tokens, Graph graph, int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, string fileName, GrouperAlias grouperAlias, SorterAlias sorterAlias, int fixedArraySize, bool allowPrint) 
        {
            CheckArgs(tokens, graph, threadCount, printer, formater, verticesPerThread, fileName, fixedArraySize);
            CheckAliases(grouperAlias, sorterAlias, mode);

            if (mode == QueryMode.HalfStreamed)
                return new Query(tokens, graph, allowPrint, threadCount, printer, formater, verticesPerThread, fileName, grouperAlias, sorterAlias, fixedArraySize, false);
            else if (mode == QueryMode.Streamed) 
                return new Query(tokens, graph, allowPrint,threadCount, printer, formater, verticesPerThread, fileName, grouperAlias, sorterAlias, fixedArraySize, true);
            else return new Query(tokens, graph, allowPrint ,threadCount, printer, formater, verticesPerThread, fileName, grouperAlias, sorterAlias, fixedArraySize);
        }

        private static void CheckArgs(Object inputQuery, Graph graph, int threadCount, PrinterType printer, FormaterType formater, int verticesPerThread, string fileName, int fixedArraySize)
        {
            if (inputQuery == null || graph == null)
                throw new ArgumentException("Query, input query or graph cannot be null.");
            else if (threadCount <= 0 || verticesPerThread <= 0)
                throw new ArgumentException("Query, thread count and vertices per thread cannot be <= 0.");
            else if (fixedArraySize <= 0)
                throw new ArgumentException("Query, invalid number of fixed array size.");
            else return;
        }

        public static void CheckAliases(GrouperAlias grouperAlias, SorterAlias sorterAlias, QueryMode mode)
        {
            if (mode == QueryMode.HalfStreamed)
            {
                if (!Aliases.HalfStreamedGroupers.Contains(grouperAlias))
                    throw new ArgumentException("Query HS, invalid grouper alias.");
                else if (!Aliases.HalfStreamedSorters.Contains(sorterAlias))
                    throw new ArgumentException("Query HS, invalid sorter alias.");
                else { }
            }
            else if (mode == QueryMode.Streamed)
            {
                if (!Aliases.StreamedGroupers.Contains(grouperAlias))
                    throw new ArgumentException("Query S, invalid grouper alias.");
                else if (!Aliases.StreamedSorters.Contains(sorterAlias))
                    throw new ArgumentException("Query S, invalid sorter alias.");
                else { }
            }
            else if (mode == QueryMode.Normal)
            {
                if (!Aliases.NormalGroupers.Contains(grouperAlias))
                    throw new ArgumentException("Query N, invalid grouper alias.");
                else if (!Aliases.NormalSorters.Contains(sorterAlias))
                    throw new ArgumentException("Query N, invalid sorter alias.");
                else { }
            }
            else throw new ArgumentException("Query, invalid mode type.");
        }
    }

}
