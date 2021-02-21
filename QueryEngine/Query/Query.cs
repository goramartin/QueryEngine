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
    /// <summary>
    /// Represents a pgql query.
    /// It tokenizes, parses and constructs simple execution chain.
    /// Provides api to execute the query.
    /// </summary>
    sealed class Query
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
        private Query(List<Token> tokens, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            this.graph = graph;
            this.variableMap = new VariableMap(); 
            this.qEhelper = new QueryExecutionHelper(threadCount, printer, formater, verticesPerThread, 4_194_304, fileName, "DFSParallel", "DFSSingleThread", "SIMPLE", "refL", "mergeSort");

            // Parse input query.
            var parsedClauses = Parser.Parse(tokens);

            // Create execution chain. 
            if (parsedClauses.ContainsKey("groupby") && parsedClauses.ContainsKey("orderby"))
                throw new ArgumentException($"{this.GetType()}, the query cannot contain both order by and group by");
            QueryObject groupBy = null;
            QueryObject orderBy = null;
            
            // MATCH is always leaf.
            QueryObject match = QueryObject.Factory
                (typeof(MatchObject), graph, qEhelper, variableMap, parsedClauses["match"], null);

            // Second must be group by because it defines what can be in other clauses.
            // GROUP BY
            if (parsedClauses.ContainsKey("groupby"))
            {
                this.exprInfo = new QueryExpressionInfo(true);
                groupBy = QueryObject.Factory(typeof(GroupByObject), graph, qEhelper, variableMap, parsedClauses["groupby"], exprInfo);
            }
            else this.exprInfo = new QueryExpressionInfo(false);

            // SELECT is the last one to process the resuls.
            this.query = QueryObject.Factory
                (typeof(SelectObject), graph, qEhelper, variableMap, parsedClauses["select"], exprInfo);

            // Check if the results are in a single group.
            this.SetSingleGroupFlags();

            // ORDER BY
            if (parsedClauses.ContainsKey("orderby"))
            {
                orderBy = QueryObject.Factory(typeof(OrderByObject), graph, qEhelper, variableMap, parsedClauses["orderby"], exprInfo);
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
        private Query(List<Token> tokens, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName, bool isStreamed)
        {
            this.graph = graph;
            this.variableMap = new VariableMap();
            this.qEhelper = new QueryExecutionHelper(threadCount, printer, formater, verticesPerThread, 4_194_304, fileName, "DFSParallelStreamed", "DFSSingleThreadStreamed", "SIMPLE", "globalS", "abtreeS");

            // Parse input query.
            var parsedClauses = Parser.Parse(tokens);
            if (parsedClauses.ContainsKey("orderby") && parsedClauses.ContainsKey("groupby"))
                throw new ArgumentException($"{this.GetType()}, the streamed version of the query cannot contain group by and order by at the same time.");

            // MATCH is always leaf.
            MatchObjectStreamed match = (MatchObjectStreamed)QueryObject.Factory
                 (typeof(MatchObjectStreamed), graph, qEhelper, variableMap, parsedClauses["match"], null);

            // GROUP BY and obtain the aggregates and hashes -> the all necessary info is in the expressionInfo class. 
            if (parsedClauses.ContainsKey("groupby"))
            {
                this.exprInfo = new QueryExpressionInfo(true);
                GroupResultProcessor.ParseGroupBy(graph, variableMap, qEhelper, (GroupByNode)parsedClauses["groupby"], exprInfo);
            }
            else this.exprInfo = new QueryExpressionInfo(false);

            // SELECT is the last one to process the resuls.
            this.query = QueryObject.Factory
                (typeof(SelectObject), graph, qEhelper, variableMap, parsedClauses["select"], exprInfo);
            
            SetSingleGroupFlags();

            // ORDER BY
            if (parsedClauses.ContainsKey("orderby"))
            {
                var orderByProc = OrderByResultProcessor.Factory(graph, variableMap, qEhelper, (OrderByNode)parsedClauses["orderby"], exprInfo, variableMap.GetCount());
                match.PassResultProcessor(orderByProc);
            } else
            {
                // Check if the query is aggregation and not a simple query.
                var groupByProc = GroupResultProcessor.Factory(exprInfo, qEhelper, variableMap.GetCount());
                if ((this.exprInfo.Aggregates.Count == 0 && this.qEhelper.IsSetSingleGroupGroupBy) || (!this.qEhelper.IsSetSingleGroupGroupBy && !parsedClauses.ContainsKey("groupby")))
                throw new ArgumentException($"{this.GetType()}, no grouping was specified. The streamed version allows to compute only aggregations.");
                
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

        /// <summary>
        /// Builds a query from an input string.
        /// </summary>
        /// <param name="inputQuery"> An input query. </param>
        /// <param name="graph"> A graph to compute the query on. </param>
        /// <param name="threadCount"> Maximum number of threads available to use. </param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName"> A file to store results into. </param>
        public static Query Create(string inputQuery, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            CheckArgs(inputQuery, graph, threadCount, printer, formater, verticesPerThread, fileName);
            return new Query(Tokenizer.Tokenize(inputQuery), graph, threadCount, printer, formater, verticesPerThread, fileName);
        }
        /// <summary>
        /// Builds a query from an input stream.
        /// </summary>
        /// <param name="inputQuery"> An input query. </param>
        /// <param name="graph"> A graph to compute the query on. </param>
        /// <param name="threadCount"> Maximum number of threads available to use. </param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName"> A file to store results into. </param>
        public static Query Create(TextReader inputQuery, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            CheckArgs(inputQuery, graph, threadCount, printer, formater, verticesPerThread, fileName);
            return new Query(Tokenizer.Tokenize(inputQuery), graph, threadCount, printer, formater, verticesPerThread, fileName);
        }


        /// <summary>
        /// Builds a streamed version of a query from an input string.
        /// </summary>
        /// <param name="inputQuery"> An input query. </param>
        /// <param name="graph"> A graph to compute the query on. </param>
        /// <param name="threadCount"> Maximum number of threads available to use. </param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName"> A file to store results into. </param>
        public static Query CreateStreamed(string inputQuery, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            CheckArgs(inputQuery, graph, threadCount, printer, formater, verticesPerThread, fileName);
            return new Query(Tokenizer.Tokenize(inputQuery), graph, threadCount, printer, formater, verticesPerThread, fileName, true);
        }
        /// <summary>
        /// Builds a streamed version of a query from an input stream.
        /// </summary>
        /// <param name="inputQuery"> An input query. </param>
        /// <param name="graph"> A graph to compute the query on. </param>
        /// <param name="threadCount"> Maximum number of threads available to use. </param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName"> A file to store results into. </param>
        public static Query CreateStreamed(TextReader inputQuery, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            CheckArgs(inputQuery, graph, threadCount, printer, formater, verticesPerThread, fileName);
            return new Query(Tokenizer.Tokenize(inputQuery), graph, threadCount, printer, formater, verticesPerThread, fileName, true);
        }

        /// <summary>
        /// Checks arguments of constructors.
        /// Called from static create method.
        /// </summary>
        private static void CheckArgs(Object inputQuery, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            if (inputQuery == null || graph == null)
                throw new ArgumentException("Query, input query or graph cannot be null.");
            else if (threadCount <= 0 || verticesPerThread <= 0)
                throw new ArgumentException("Query, thread count and vertices per thread cannot be <= 0.");
            else if (!Printer.Printers.Contains(printer) || !Formater.Formaters.Contains(formater))
                throw new ArgumentException("Query, invalid printer or formater.");
            else return;
        }
    }

}
