/*! \file
This file includes definition of one query. 
This could be considered as a facade because it includes classes that form more complicated 
structures.
Query is formed by query objects, those are match object, select object, order by ...
Those objects represent information parsed from the inputted query.
They also perform the duties with relation to their semantic meaning, such as, match object conducts matching on the graph,
select prints results to the output and orderby sorts the results.

The query is given a graph to compute the query on, a reader that reads user input query, a query execution helper that provides neccessary information
for the query object (for example the number of threads available).

The query itself is constructed as follows.
Firstly the user input is tokenized from the reader. Then parsed trees are created from the tokens.
Parsed trees are passed to constructors of each query object.

The query objects form an simple execution plan, where the objects that has to finish first are put at the end of the chains and vice versa.
The user calls compute on the query and the chains calls recursively to the last object. When object finished, it passes arguments in 
the out parameter and the computation continues.

Some query clauses must be present every time. Those are the select and the match clause.
Other clauses are purely optional. Those are group by and order by.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom;

namespace QueryEngine
{
    /// <summary>
    /// Represents a pgql query.
    /// It tokenizes, parses and constructs simple execution chain.
    /// Provides api to execute the query.
    /// More information is at the head of the file.
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
        /// Builds a query from input.
        /// Called by static create method.
        /// </summary>
        /// <param name="inputQuery"> An input query. </param>
        /// <param name="graph"> A graph to compute the query on. </param>
        /// <param name="threadCount"> Maximum number of threads available to use. </param>
        /// <param name="printer"> A printer to use. </param>
        /// <param name="formater"> A formater to use by printer. </param>
        /// <param name="verticesPerThread"> A number of vertices distributed to threads during parallel computation of the query.</param>
        /// <param name="fileName"> A file to store results into. </param>
        private Query(List<Token> tokens, Graph graph, int threadCount, string printer, string formater, int verticesPerThread, string fileName)
        {
            this.graph = graph;
            this.variableMap = new VariableMap();
            this.qEhelper = new QueryExecutionHelper();
            this.qEhelper.ThreadCount = threadCount;
            this.qEhelper.Printer = printer;
            this.qEhelper.Formater = formater;
            this.qEhelper.VerticesPerThread = verticesPerThread;
            this.qEhelper.FileName = fileName;

            // Parse input query.
            var parsedClauses = Parser.Parse(tokens);

            // Create execution chain. // 
            if (parsedClauses.ContainsKey("groupby") && parsedClauses.ContainsKey("orderby"))
                throw new ArgumentException($"{this.GetType()}, query cannot contain both order by and group by");
            QueryObject groupBy = null;
            QueryObject orderBy = null;
            
            
            // MATCH is always leaf.
            QueryObject match = QueryObject.Factory
                (typeof(MatchObject), graph, qEhelper, variableMap, parsedClauses["match"], exprInfo);

            // Second must be group by because it defines what can be in other clauses.
            // GROUP BY
            if (parsedClauses.ContainsKey("groupby"))
            {
                groupBy = QueryObject.Factory(typeof(GroupByObject), graph, qEhelper, variableMap, parsedClauses["groupby"], exprInfo);
                this.exprInfo = new QueryExpressionInfo(true);
            }
            else this.exprInfo = new QueryExpressionInfo(false);

            // SELECT is the last one to process the resuls.
            this.query = QueryObject.Factory
                (typeof(SelectObject), graph, qEhelper, variableMap, parsedClauses["select"], exprInfo) ;

            // Check if the results are in a single group.
            this.SetSingleGroupFlags();

            // ORDER BY
            if (parsedClauses.ContainsKey("orderby"))
            {
                orderBy = QueryObject.Factory(typeof(OrderByObject), graph, qEhelper, variableMap, parsedClauses["orderby"], exprInfo);
                query.AddToEnd(orderBy);
            }

            // If the single group by is set, add 
            if (this.qEhelper.IsSetSingleGroupGroupBy && !this.qEhelper.IsSetGroupBy)
                groupBy = QueryObject.Factory(typeof(GroupByObject), null, qEhelper, null, null, exprInfo);

            if (groupBy != null) query.AddToEnd(groupBy);
            query.AddToEnd(match);
        }

        /// <summary>
        /// Computes a query.
        /// </summary>
        public void Compute()
        {
            if (!this.Finished)
            {
                this.query.Compute(out ITableResults res);
                this.Finished = true;
            }
        }

        /// <summary>
        /// Sets flags to execution helper after the definition of group by and select.
        /// The flag should be set to false, in case there is not set group by and the select clause
        /// contains only count(*) in that case the results are not needed. Otherwise they are needed.
        /// Note that this sets to false even though the order by is set.
        /// </summary>
        private void SetSingleGroupFlags()
        {
            // No group by
            if (!this.qEhelper.IsSetGroupBy)
            {
                // There are only aggregates
                if (this.exprInfo.aggregates.Count > 0)
                {
                    bool foundNonAst = false;
                    for (int i = 0; i < this.exprInfo.aggregates.Count; i++)
                        if (!this.exprInfo.aggregates[i].IsAstCount) foundNonAst = true;
                    
                    // If there are only count(*) the results are not needed.
                    if (!foundNonAst ) this.qEhelper.IsStoringResult = false;
                    this.qEhelper.IsSetSingleGroupGroupBy = true;
                }
            }
        }

        /// <summary>
        /// Builds a query from input.
        /// Calls private constructor.
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
        /// Builds a query from input.
        /// Calls private constructor.
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
        /// Checks arguments of constructors.
        /// Called from static create method.
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
