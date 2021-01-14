
/*! \file
This file contains definitions of a Parser.
(Sometimes in comments there are used "o-" instead of "<-" because it destroys xml formatting)
  
Parsing is done via Deep descend parsing (Top to bottom).
The whole query expression forms a single tree. Each parser method (ParseSelect, ParseMatch)
parses only the part corresponding to the query word and returns a parse tree of that expression.
Visitors then create structures that are used to create query objects.

Grammar:
Query -> Select Match (OrderBy)? ;
  
Select -> SELECT (\*|(SelectPrintTerm (, SelectPrintTerm)*)
SelectPrintTerm -> Expression

Match -> MATCH MatchTerm (, MatchTerm)*
MatchTerm -> Vertex (Edge Vertex)*
Vertex -> (MatchVariable)
Edge -> (EmptyAnyEdge|EmptyOutEdge|EmptyInEdge|AnyEdge|InEdge|OutEdge) 
EmptyAnyEdge -> -
EmptyOutEdge -> <-
EmptyInEdge -> ->
AnyEdge -> -[MatchVariable]-
InEdge -> <-[MatchVariable]-
OutEdge -> -[MatchVariable]->
MatchVariable -> (VariableNameReference)?(:TableType)?
TableType -> IDENTIFIER
 
OrderBy -> ORDER BY OrderTerm (, OrderTerm)*
OrderTerm -> Expression (ASC|DESC)?

GroupBy -> GroupByTerm (, GroupByTerm)*
GroupByTerm -> Expression

Expression -> VariableNameReference(.VariablePropertyReference)? AS Label
Label -> IDENTIFIER
VariableNameReference -> IDENTIFIER
VariablePropertyReference -> IDENTIFIER
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{
    /// <summary>
    /// Creates query tree from tokens. Using deep descend parsing method. Top -> Bottom method.
    /// Each query words is parsed separately.
    /// Parsing should always start with parsing select and match
    /// since they are compulsory to use.
    /// When finished parsing query token, the position is set on the next token.
    /// Query -> Select Match (OrderBy)? ;
    /// </summary>
    internal static partial class Parser
    {
        private delegate Node ParsePart(ref int p, List<Token> tokens);
        private static List<Tuple<string, ParsePart>> parts;

        static Parser() {
            parts = new List<Tuple<string, ParsePart>>();
            parts.Add(Tuple.Create<string, ParsePart>("select", Parser.ParseSelect));
            parts.Add(Tuple.Create<string, ParsePart>("match", Parser.ParseMatch));
            parts.Add(Tuple.Create<string, ParsePart>("groupby", Parser.ParseGroupBy));
            parts.Add(Tuple.Create<string, ParsePart>("orderby", Parser.ParseOrderBy));
        }

        /// <summary>
        /// Parses inputed list of tokens and creates corresponding parse trees for 
        /// the query expressions.
        /// Order of the parsing of tokens is given precisely, and defined in the static constructor of the parser.
        /// </summary>
        /// <param name="tokens"> A list of tokens that were parsed from a string/console. </param>
        /// <returns> A dictionary of parsed query expressions with corresponding label. So that the class that 
        /// processes the expression can pick which one to process.</returns>
        static public Dictionary<string, Node> Parse(List<Token> tokens)
        {
            var parsedParts = new Dictionary<string, Node>();

            int position = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                Node parseTree = parts[i].Item2(ref position, tokens);
                if (parseTree != null) parsedParts.Add(parts[i].Item1, parseTree);
            }

            if (position != tokens.Count) ThrowError("Parser", "failed to parse every token", position, tokens);
            
            return parsedParts;
        }


        /// <summary>
        /// Check for token on position given.
        /// </summary>
        /// <param name="p"> Position in list of tokens </param>
        /// <param name="type"> Type of token to be checked against </param>
        /// <param name="tokens"> List of parse tokens </param>
        /// <returns></returns>
        static private bool CheckToken(int p, Token.TokenType type, List<Token> tokens)
        {
            if (p < tokens.Count && tokens[p].type == type)
            {
                return true;
            }
            return false;

        }

        /// <summary>
        /// Builds error message.
        /// Each parser type passes it is own type and a message.
        /// Afterwards tokens are printed.
        /// </summary>
        /// <param name="parserType"> Type of parser. </param>
        /// <param name="message"> A message to print. </param>
        /// <param name="position"> A position of error. </param>
        /// <param name="tokens"> Parsed tokens. </param>
        private static void ThrowError(string parserType, string message, int position, List<Token> tokens)
        {
            string msg = parserType + ": " + message + " At " + position + " | Tokens:";

            for (int i = 0; i < tokens.Count; i++)
            {
                if (i != position) msg += " " + i + ": " + tokens[i].ToString() + " ";
                else msg += " (" + i + ": " + tokens[i].ToString() + ") ";
            }
            throw new ParserException(msg);
        }

        public class ParserException : Exception
        {
            public ParserException()
            {
            }

            public ParserException(string message)
                : base(message)
            {
            }

            public ParserException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }


    }

}

