
/**
 * This file includes definition of one query. 
 * This could be considered as a facade because it includes classes that form more complicated 
 * structures.
 * Query is formed by query objects, those are match object, select object ...
 * Those objects represents information parsed from the inputted query.
 * They also perform the duties that involves eg implementing search algorithm for match object or
 * printing results in defined fashion for select object.
 * */




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
    class Query
    {
        VariableMap variableMap;
        SelectObject select;
        MatchObject match;
        QueryResults results;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"> Input of query. </param>
        /// <param name="graph"> Graph to be conduct a query on. </param>
        public Query(TextReader reader, Graph graph)
        {
            if (reader == null || graph == null) throw new ArgumentException($"{this.GetType()} Passed null as a reader or graph.");

            // Create tokens from console.
            List<Token> tokens = Tokenizer.Tokenize(reader);

            // Parse and create in order of the query words.
            Parser.ResetPosition();
            this.variableMap = new VariableMap();
            this.select = new SelectObject(tokens);
            
            this.match = new MatchObject(tokens, variableMap, graph);

            // Check correctness of select part
            select.CheckCorrectnessOfSelect(variableMap);

            // Check if it successfully parsed every token.
            if (tokens.Count != Parser.GetPosition())
                throw new ArgumentException("Failed to parse every token for Query.");
        }

    }

}
