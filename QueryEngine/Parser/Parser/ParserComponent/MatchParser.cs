using System;
using System.Collections.Generic;

namespace QueryEngine
{
   // For more info visit a file Parser.cs
   // Contains a match part of the parser.
   internal static partial class Parser
    {
        #region MATCH

        /// <summary>
        /// Parsing a match expression, chains of vertex -> edge -> vertex expressions.
        /// Match -> MATCH MatchTerm (, MatchTerm)*
        /// MatchTerm -> Vertex (Edge Vertex)*
        /// Vertex -> (MatchVariable)
        /// Edge -> (EmptyAnyEdge|EmptyOutEdge|EmptyInEdge|AnyEdge|InEdge|OutEdge) 
        /// EmptyAnyEdge -> -
        /// EmptyOutEdge -> o-
        /// EmptyInEdge -> ->
        /// AnyEdge -> -[MatchVariable]-
        /// InEdge -> o-[MatchVariable]-
        /// OutEdge -> -[MatchVariable]->
        /// MatchVariable -> (VariableNameReference)?(:TableType)?
        /// TableType -> IDENTIFIER
        /// VariableNameReference -> IDENTIFIER
        /// </summary>
        /// <returns> A tree representation of Match expression. </returns>
        static public MatchNode ParseMatch(ref int position, List<Token> tokens)
        {
            MatchNode matchNode = new MatchNode();

            // We expect after reading Select expr that the position is set on the Match token.
            if (!CheckToken(position, Token.TokenType.Match, tokens))
                ThrowError("Match parser", "Failed to find MATCH token.", position, tokens);
            else
            {
                position++;
                Node node = ParseVertex(ref position, tokens);
                if (node == null) ThrowError("Match parser", "Failed to parse Match expression.", position, tokens);
                matchNode.AddNext(node);
            }
            return matchNode;

        }

        /// <summary>
        /// Parses variable enclosed in vertex or edge.
        /// Expects  Name:Type / Name / :Type / (nothing)
        /// </summary>
        /// <returns> A variable node. </returns>
        static private Node ParseMatchVariable(ref int position, List<Token> tokens)
        {
            MatchVariableNode matchVariableNode = new MatchVariableNode();

            // Expecting identifier, name of variable. Can be empty, if so then it is anonymous variable.
            Node name = ParseIdentifierExrp(ref position, tokens);
            if (name != null) position++;
            matchVariableNode.AddVariableName(name);

            // Check for type of vairiable after :
            if (CheckToken(position, Token.TokenType.DoubleDot, tokens))
            {
                position++;
                Node identifierNode = ParseIdentifierExrp(ref position, tokens);
                if (identifierNode == null) ThrowError("Match parser", "Expected IDENTIFIER after double dot.", position, tokens);
                else matchVariableNode.AddVariableType(identifierNode);
                position++;
            }

            if (matchVariableNode.IsEmpty()) return null;
            else return matchVariableNode;
        }

        /// <summary>
        /// Parses vertex node.
        /// </summary>
        /// <returns> A vertex node. </returns>
        static private Node ParseVertex(ref int position, List<Token> tokens)
        {
            VertexNode vertexNode = new VertexNode();

            CheckLeftParen(ref position, tokens);
            // Parse Values of the variable.
            Node variableNode = ParseMatchVariable(ref position, tokens);
            vertexNode.AddMatchVariable(variableNode);
            CheckRightParen(ref position, tokens);

            // Position incremented from leaving function PsrseVariable.
            // Try parse an Edge.
            Node edgeNode = ParseEdge(ref position, tokens);
            if (edgeNode != null)
            {
                vertexNode.AddNext(edgeNode);
                return vertexNode;
            }

            // Try Parse another pattern, divided by comma.
            Node newPattern = ParseNewPatternExpr(ref position, tokens);
            if (newPattern != null) vertexNode.AddNext(newPattern);

            // Always must return valid vertex.
            return vertexNode;
        }


        /// <summary>
        /// Parses edge expression.
        /// First parsing anonymous edge is tried and then edges that define variables.
        /// </summary>
        /// <returns> A chain of vertex/edge nodes. </returns>
        static private Node ParseEdge(ref int position, List<Token> tokens)
        {
            EdgeNode edgeNode = null;

            edgeNode = (EdgeNode)TryParseEmptyEdge(ref position, tokens);
            if (edgeNode == null)
            {
                edgeNode = (EdgeNode)ParseEdgeWithMatchVariable(ref position, tokens);
                if (edgeNode == null) return null;
            }

            Node vertexNode = ParseVertex(ref position, tokens);
            if (vertexNode != null) edgeNode.AddNext(vertexNode);
            else ThrowError("Match parser", "Expected Vertex.", position, tokens);

            return edgeNode;

        }
        /// <summary>
        /// Tries to parse an anonymous edge type.
        /// </summary>
        /// <returns> An anonymous edge or null. </returns>
        static private Node TryParseEmptyEdge(ref int position,List<Token> tokens)
        {
            EdgeNode edgeNode;
            if (CheckEmptyAnyEdge(ref position, tokens))
            {
                edgeNode = new AnyEdgeNode();
                position++;
                return edgeNode;
            }
            else if (CheckEmptyOutEdge(ref position, tokens))
            {
                edgeNode = new OutEdgeNode();
                position = position + 2;
                return edgeNode;
            }
            else if (CheckEmptyInEdge(ref position, tokens))
            {
                edgeNode = new InEdgeNode();
                position = position + 2;
                return edgeNode;
            }
            else return null;
        }

        /// <summary>
        /// Check tokens for an anonymous any edge.
        /// </summary>
        /// <returns> True on matched edge. </returns>
        private static bool CheckEmptyAnyEdge(ref int position, List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                CheckToken(position + 1, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }

        /// <summary>
        /// Check tokens for an anonymous out edge.
        /// </summary>
        /// <returns> True on matched edge. </returns>
        private static bool CheckEmptyOutEdge(ref int position, List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                CheckToken(position + 1, Token.TokenType.Greater, tokens) &&
                CheckToken(position + 2, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }

        /// <summary>
        /// Check tokens for an anonymous in edge.
        /// </summary>
        /// <returns> True on matched edge. </returns>
        private static bool CheckEmptyInEdge(ref int position, List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Less, tokens) &&
               CheckToken(position + 1, Token.TokenType.Dash, tokens) &&
               CheckToken(position + 2, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }


        /// <summary>
        /// Parses an edge expression with an enclosed variable definition.
        /// </summary>
        /// <returns> Null on fault match or edge node.</returns>
        private static Node ParseEdgeWithMatchVariable(ref int position, List<Token> tokens)
        {
            EdgeNode edgeNode = null;

            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                position++;
                edgeNode = (EdgeNode)ParseOutAnyEdge(ref position, tokens);
            }
            else if (CheckToken(position, Token.TokenType.Less, tokens))
            {
                position++;
                edgeNode = (EdgeNode)ParseInEdge(ref position, tokens);
            }
            else edgeNode = null;

            return edgeNode;
        }


        /// <summary>
        /// Parses an out or any edge expression with an enclosed variable definition.
        /// Throws on mis match.
        /// </summary>
        /// <returns> An edge node.</returns>
        private static Node ParseOutAnyEdge(ref int position, List<Token> tokens)
        {
            EdgeNode edgeNode;
            MatchVariableNode matchVariableNode;

            CheckLeftBrace(ref position, tokens);
            matchVariableNode = (MatchVariableNode)(ParseMatchVariable(ref position, tokens));
            CheckRightBrace(ref position, tokens);


            // -> || -
            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                position++;
                edgeNode = new AnyEdgeNode();
                if (CheckToken(position, Token.TokenType.Greater, tokens))
                {
                    position++;
                    edgeNode = new OutEdgeNode();
                }
            }
            else throw new ArgumentException($"EdgeParser, expected ending of edge.");

            edgeNode.AddMatchVariable(matchVariableNode);
            return edgeNode;
        }
        /// <summary>
        /// Parses an in edge expression with an enclosed variable definition.
        /// Throws on mis match.
        /// </summary>
        /// <returns> An edge node.</returns>
        private static Node ParseInEdge(ref int position, List<Token> tokens)
        {
            EdgeNode edgeNode;
            MatchVariableNode matchVariableNode;

            // -
            if (!CheckToken(position, Token.TokenType.Dash, tokens))
                throw new ArgumentException($"EdgeParser, expected beginning of edge.");
            else position++;

            CheckLeftBrace(ref position, tokens);
            matchVariableNode = (MatchVariableNode)(ParseMatchVariable(ref position, tokens));
            CheckRightBrace(ref position, tokens);

            // -
            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                position++;
                edgeNode = new InEdgeNode();
            }
            else throw new ArgumentException($"MatchParser, expected ending of edge.");

            edgeNode.AddMatchVariable(matchVariableNode);
            return edgeNode;
        }

        /// <summary>
        /// Check a token for left brace.
        /// Throws on mismatch.
        /// </summary>
        /// <returns> True on the matched brace.</returns>
        private static void CheckLeftBrace(ref int position, List<Token> tokens)
        {
            // [
            if (CheckToken(position, Token.TokenType.LeftBrace, tokens)) position++;
            else ThrowError("Match parser", "Expected [.", position, tokens);

        }
        /// <summary>
        /// Check a token for right brace.
        /// Throws on mismatch.
        /// </summary>
        /// <returns> True on the matched brace.</returns>
        private static void CheckRightBrace(ref int position, List<Token> tokens)
        {
            // ]
            if (CheckToken(position, Token.TokenType.RightBrace, tokens)) position++;
            else ThrowError("Match parser", "Expected ].", position, tokens);

        }
        /// <summary>
        /// Check a token for left parent.
        /// Throws on mismatch.
        /// </summary>
        /// <returns> True on matched parent.</returns>
        private static void CheckLeftParen(ref int position, List<Token> tokens)
        {
            // (
            if (CheckToken(position, Token.TokenType.LeftParen, tokens)) position++;
            else ThrowError("Match parser", "Expected (.", position, tokens);
        }
        /// <summary>
        /// Check a token for left parent.
        /// Throws on mismatch.
        /// </summary>
        /// <returns> True on matched parent.</returns>
        private static void CheckRightParen(ref int position, List<Token> tokens)
        {
            // )
            if (CheckToken(position, Token.TokenType.RightParen, tokens)) position++;
            else ThrowError("Match parser", "Expected ).", position, tokens);
        }

        /// <summary>
        /// Tries whether after an vertex there is a comma, if there is the comma, that means there are more patterns to parse.
        /// </summary>
        /// <returns> Start of a new pattern. </returns>
        static private Node ParseNewPatternExpr(ref int position, List<Token> tokens)
        {
            // Checks for comma, after comma next pattern must be
            if (!CheckToken(position, Token.TokenType.Comma, tokens)) return null;
            position++;

            MatchDividerNode matchDivider = new MatchDividerNode();


            Node newPattern = ParseVertex(ref position, tokens);
            if (newPattern == null) ThrowError("Match parser", "Expected Vertex after comma.", position, tokens);
            matchDivider.AddNext(newPattern);
            return matchDivider;
        }


        #endregion MATCH
    }
}
