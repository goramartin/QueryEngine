/*! \file
This file contains definitions of a Parser.
(Sometimes in comments there are used "o-" instead of "<-" because it destroys xml formatting)
  
Parsing is done via deep descend parsing (Top to bottom).
Each parser method (ParseSelect, ParseMatch) parses only a part corresponding to the query word and returns it's parse tree.
Visitors then create structures that are used to create query objects.

Grammar (capital are terminals, using regular expressions to make it more readable):
Query: Select Match Optional
Optional: OrderBy | GroupBy
  
Select: SELECT (\* | (SelectPrintTerm (, SelectPrintTerm)*)
SelectPrintTerm: Expression

Match: MATCH MatchTerm (, MatchTerm)*
MatchTerm: Vertex (Edge Vertex)*
Vertex: (MatchVariable)
Edge: (EmptyAnyEdge|EmptyOutEdge|EmptyInEdge|AnyEdge|InEdge|OutEdge) 
EmptyAnyEdge: -
EmptyOutEdge: <-
EmptyInEdge: ->
AnyEdge: -\[MatchVariable\]-
InEdge: <-\[MatchVariable\]-
OutEdge: -\[MatchVariable\]->
MatchVariable: | VariableNameReference | :TableType | VariableNameReference:TableType
TableType: IDENTIFIER
VariableNameReference: IDENTIFIER
 
OrderBy: ORDER BY OrderTerm (, OrderTerm)*
OrderTerm: Expression (ASC | DESC)?

GroupBy: GroupByTerm (, GroupByTerm)*
GroupByTerm: Expression

Expression -> ExpressionTerm AS Label
ExpressionTerm -> AggregateFunc|VarReference
AggregateFunc -> IDENTIFIER \( VarReference \)
VarReference -> ReferenceName(.ReferenceName)?
Label -> IDENTIFIER
ReferenceName -> IDENTIFIER
*/

using System;
using System.Collections.Generic;

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
        private static List<Tuple<Clause, ParsePart>> parts;
        public enum Clause { Select, Match, GroupBy, OrderBy }


        static Parser() {
            parts = new List<Tuple<Clause, ParsePart>>();
            parts.Add(Tuple.Create<Clause, ParsePart>(Clause.Select, Parser.ParseSelect));
            parts.Add(Tuple.Create<Clause, ParsePart>(Clause.Match, Parser.ParseMatch));
            parts.Add(Tuple.Create<Clause, ParsePart>(Clause.GroupBy, Parser.ParseGroupBy));
            parts.Add(Tuple.Create<Clause, ParsePart>(Clause.OrderBy, Parser.ParseOrderBy));
        }

        /// <summary>
        /// Parses inputed List of tokens and creates corresponding parse trees for 
        /// the query expressions.
        /// Order of the parsing of tokens is given precisely, and defined in the static constructor of the parser.
        /// </summary>
        /// <param name="tokens"> A List of tokens that were parsed from a string/console. </param>
        /// <returns> A 
        /// of parsed query expressions with corresponding label. So that the class that 
        /// processes the expression can pick which one to process.</returns>
        static public Dictionary<Clause, Node> Parse(List<Token> tokens)
        {
            if (tokens.Count == 0)
                ThrowError("Parser", "the inputted query is empty", 0, tokens);


            var parsedParts = new Dictionary<Clause, Node>();

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
        /// Check for a token on the given position given.
        /// </summary>
        static private bool CheckToken(int p, Token.TokenType type, List<Token> tokens)
        {
            if (p < tokens.Count && tokens[p].type == type)
            {
                return true;
            }
            return false;

        }

        /// <summary>
        /// Builds an error message.
        /// Each parser type passes has it is own type and a message.
        /// </summary>
        /// <param name="parserType"> A type of parser. </param>
        /// <param name="message"> A message to print. </param>
        /// <param name="position"> A position of error. </param>
        /// <param name="tokens"> Parsed tokens. </param>
        private static void ThrowError(string parserType, string message, int position, List<Token> tokens)
        {
            string msg = parserType + ": " + message + ". Token position: " + position + " Tokens:";

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
            { }

            public ParserException(string message)
                : base(message)
            { }

            public ParserException(string message, Exception inner)
                : base(message, inner)
            { }
        }


    }

}

