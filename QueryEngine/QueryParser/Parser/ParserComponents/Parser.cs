
/*! \file
This file contains definitions of a Parser.
(Sometimes in comments there are used "o-" instead of "<-" because it destroys xml formatting)
  
Parsing is done via Deep descend parsing (Top to bottom).
The whole query expression forms a single tree. Each parser method (ParseSelectExpr, ParseMatchExpr...)
parses only the part corresponding to the query word and leaves the internal position of the next parsed token
to the next token after the last token parsed by methods above.
  
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
    /// Parsing Select always starts at position 0.
    /// When finished parsing query token, the position is set on the next token.
    /// Query -> Select Match (OrderBy)? ;
    /// </summary>
    internal static partial class Parser
    {


        private delegate Node ParsePart(ref int p, List<Token> tokens);
        private static Dictionary<string, ParsePart> parserParts;

        static Parser() { 
        
        
        
        
        
        
        }


        /// <summary>
        /// Check for token on position given.
        /// 
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
    }

}

