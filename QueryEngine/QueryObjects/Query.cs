
/*! \file
  This file includes definition of one query. 
  This could be considered as a facade because it includes classes that form more complicated 
  structures.
  Query is formed by query objects, those are match object, select object ...
  Those objects represents information parsed from the inputted query.
  They also perform the duties that involves eg implementing search algorithm for match object or
  printing results in defined fashion for select object.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace QueryEngine
{
    /// <summary>
    /// Query represents query information, carrying it in each of the query objects.
    /// It is a facade for the query.
    /// </summary>
    sealed class Query
    {
        VariableMap variableMap;
        SelectObject select;
        MatchObject match;
        OrderByObject orderBy;
        IResults results;
        QueryExecutionHelper qEhelper;

        /// <summary>
        /// Creates all neccessary object for query.
        /// </summary>
        /// <param name="reader"> Input of query. </param>
        /// <param name="graph"> Graph to be conduct a query on. </param>
        /// <param name="executionHelper"> Helper for query execution. Basically, parsed arguments from user. </param>
        public Query(TextReader reader, Graph graph, QueryExecutionHelper executionHelper)
        {
            if (reader == null || graph == null) throw new ArgumentException($"{this.GetType()} Passed null as a reader or graph.");
            this.qEhelper = executionHelper;

            // Create tokens from console.
            List<Token> tokens = Tokenizer.Tokenize(reader);

            // Parse and create main object in order of the query words.
            Parser.ResetPosition();
            this.variableMap = new VariableMap();

            SelectNode selectNode = Parser.ParseSelect(tokens);
            this.match = new MatchObject(tokens, variableMap, graph, this.qEhelper);
            this.select = new SelectObject(graph, variableMap, selectNode, this.qEhelper) ;

            // Optional, if ommited it returns null. 
            this.orderBy = OrderByObject.CreateOrderBy(tokens, graph, variableMap, this.qEhelper);


            // Check if it successfully parsed every token.
            if (tokens.Count != Parser.GetPosition())
                throw new ArgumentException("Failed to parse every token for Query.");
        }


        /// <summary>
        /// Computes and prints results of a query.
        /// </summary>
        public void ComputeQuery()
        {
            this.results = this.match.Search(this.qEhelper);
            this.match = null;

            if (this.qEhelper.IsSetOrderBy) this.orderBy.Sort(this.results, this.qEhelper);

            this.select.Print(this.results, this.qEhelper);
        }
    }

}
