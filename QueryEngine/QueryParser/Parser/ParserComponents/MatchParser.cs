using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
   // For more info visit a file Parser.cs
   // Contains a match part of the parser.
   internal static partial class Parser
    {
        #region MATCH

        /// <summary>
        /// Parsing Match expression, chains of vertex -> edge -> vertex expressions.
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
        /// <param name="tokens"> Token list to parse </param>
        /// <returns> Tree representation of Match expression </returns>
        static public MatchNode ParseMatch(List<Token> tokens)
        {
            MatchNode matchNode = new MatchNode();

            // We expect after reading Select expr that the position is set on the Match token.
            if (!CheckToken(position, Token.TokenType.Match, tokens))
                throw new ArgumentException("MatchParser, position is not set at Match Token.");
            else
            {
                IncrementPosition();
                Node node = ParseVertex(tokens);
                if (node == null) throw new NullReferenceException("MatchParser, Failed to parse Match Expresion.");
                matchNode.AddNext(node);
            }
            return matchNode;

        }

        /// <summary>
        /// Parses variable enclosed in vertex or edge.
        /// Expects  Name:Type / Name / :Type / (nothing)
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Variable node </returns>
        static private Node ParseMatchVariable(List<Token> tokens)
        {
            MatchVariableNode matchVariableNode = new MatchVariableNode();

            //Expecting identifier, name of variable. Can be empty, if so then it is anonymous variable.
            Node name = ParseIdentifierExrp(tokens);
            if (name != null) { IncrementPosition(); }
            matchVariableNode.AddVariableName(name);

            //Check for type of vairiable after :
            if (CheckToken(position, Token.TokenType.DoubleDot, tokens))
            {
                IncrementPosition();
                Node identifierNode = ParseIdentifierExrp(tokens);
                if (identifierNode == null) throw new NullReferenceException("MatchParser, expected Indentifier after double dot.");
                else matchVariableNode.AddVariableType(identifierNode);
                IncrementPosition();
            }

            if (matchVariableNode.IsEmpty()) return null;
            else return matchVariableNode;
        }

        /// <summary>
        /// Parses vertex node.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Vertex node </returns>
        static private Node ParseVertex(List<Token> tokens)
        {
            VertexNode vertexNode = new VertexNode();

            CheckLeftParen(tokens);
            //Parse Values of the variable.
            Node variableNode = ParseMatchVariable(tokens);
            vertexNode.AddMatchVariable(variableNode);
            CheckRightParen(tokens);

            //Position incremented from leaving function PsrseVariable.
            //Try parse an Edge.
            Node edgeNode = ParseEdge(tokens);
            if (edgeNode != null)
            {
                vertexNode.AddNext(edgeNode);
                return vertexNode;
            }

            //Try Parse another pattern, divided by comma.
            Node newPattern = ParseNewPatternExpr(tokens);
            if (newPattern != null) vertexNode.AddNext(newPattern);

            //Always must return valid vertex.
            return vertexNode;
        }


        /// <summary>
        /// Parses edge expression.
        /// First parsing anonymous edge is tried and then edges that define variables.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Chain of vertex/edge nodes. </returns>
        static private Node ParseEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = null;

            edgeNode = (EdgeNode)TryParseEmptyEdge(tokens);
            if (edgeNode == null)
            {
                edgeNode = (EdgeNode)ParseEdgeWithMatchVariable(tokens);
                if (edgeNode == null) return null;
            }

            Node vertexNode = ParseVertex(tokens);
            if (vertexNode != null) edgeNode.AddNext(vertexNode);
            else throw new NullReferenceException("MatchParser, expected vertex.");

            return edgeNode;

        }
        /// <summary>
        /// Tries to parse anonumous edge type.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Anonymous edge or null. </returns>
        static private Node TryParseEmptyEdge(List<Token> tokens)
        {
            EdgeNode edgeNode;
            if (CheckEmptyAnyEdge(tokens))
            {
                edgeNode = new AnyEdgeNode();
                IncrementPosition();
                return edgeNode;
            }
            else if (CheckEmptyOutEdge(tokens))
            {
                edgeNode = new OutEdgeNode();
                IncrementPositionBy(2);
                return edgeNode;
            }
            else if (CheckEmptyInEdge(tokens))
            {
                edgeNode = new InEdgeNode();
                IncrementPositionBy(2);
                return edgeNode;
            }
            else return null;
        }

        /// <summary>
        /// Check tokens for anonymous any edge
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static bool CheckEmptyAnyEdge(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                CheckToken(position + 1, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }

        /// <summary>
        /// Check tokens for anonymous out edge
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static bool CheckEmptyOutEdge(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                CheckToken(position + 1, Token.TokenType.Greater, tokens) &&
                CheckToken(position + 2, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }

        /// <summary>
        /// Check tokens for anonymous in edge
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static bool CheckEmptyInEdge(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Less, tokens) &&
               CheckToken(position + 1, Token.TokenType.Dash, tokens) &&
               CheckToken(position + 2, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }


        /// <summary>
        /// Parses edge expression with enclosed variable definition.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> Null on fault match or edge node.</returns>
        private static Node ParseEdgeWithMatchVariable(List<Token> tokens)
        {
            EdgeNode edgeNode = null;

            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                IncrementPosition();
                edgeNode = (EdgeNode)ParseOutAnyEdge(tokens);
            }
            else if (CheckToken(position, Token.TokenType.Less, tokens))
            {
                IncrementPosition();
                edgeNode = (EdgeNode)ParseInEdge(tokens);
            }
            else edgeNode = null;

            return edgeNode;
        }


        /// <summary>
        /// Parses out or any edge expression with enclosed variable definition.
        /// Throws on mis match.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> Edge node.</returns>
        private static Node ParseOutAnyEdge(List<Token> tokens)
        {
            EdgeNode edgeNode;
            MatchVariableNode matchVariableNode;

            CheckLeftBrace(tokens);
            matchVariableNode = (MatchVariableNode)(ParseMatchVariable(tokens));
            CheckRightBrace(tokens);


            // -> || -
            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                IncrementPosition();
                edgeNode = new AnyEdgeNode();
                if (CheckToken(position, Token.TokenType.Greater, tokens))
                {
                    IncrementPosition();
                    edgeNode = new OutEdgeNode();
                }
            }
            else throw new ArgumentException($"EdgeParser, expected ending of edge.");

            edgeNode.AddMatchVariable(matchVariableNode);
            return edgeNode;
        }
        /// <summary>
        /// Parses in edge expression with enclosed variable definition.
        /// Throws on mis match.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> Edge node.</returns>
        private static Node ParseInEdge(List<Token> tokens)
        {
            EdgeNode edgeNode;
            MatchVariableNode matchVariableNode;

            // -
            if (!CheckToken(position, Token.TokenType.Dash, tokens))
                throw new ArgumentException($"EdgeParser, expected beginning of edge.");
            else IncrementPosition();

            CheckLeftBrace(tokens);
            matchVariableNode = (MatchVariableNode)(ParseMatchVariable(tokens));
            CheckRightBrace(tokens);

            // -
            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                IncrementPosition();
                edgeNode = new InEdgeNode();
            }
            else throw new ArgumentException($"MatchParser, expected ending of edge.");

            edgeNode.AddMatchVariable(matchVariableNode);
            return edgeNode;
        }

        /// <summary>
        /// Check token for left brace
        /// Throws on mismatch.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static void CheckLeftBrace(List<Token> tokens)
        {
            // [
            if (CheckToken(position, Token.TokenType.LeftBrace, tokens)) IncrementPosition();
            else throw new ArgumentException("MatchParser variable, expected Leftbrace.");

        }
        /// <summary>
        /// Check token for right brace
        /// Throws on mismatch.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static void CheckRightBrace(List<Token> tokens)
        {
            // ]
            if (CheckToken(position, Token.TokenType.RightBrace, tokens)) IncrementPosition();
            else throw new ArgumentException("MatchParser variable, expected rightbrace.");

        }
        /// <summary>
        /// Check token for left parent
        /// Throws on mismatch.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static void CheckLeftParen(List<Token> tokens)
        {
            // (
            if (CheckToken(position, Token.TokenType.LeftParen, tokens)) IncrementPosition();
            else throw new ArgumentException("MatchParser, expected left parent.");
        }
        /// <summary>
        /// Check token for left parent.
        /// Throws on mismatch.
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static void CheckRightParen(List<Token> tokens)
        {
            // )
            if (CheckToken(position, Token.TokenType.RightParen, tokens)) IncrementPosition();
            else throw new ArgumentException("MatchParser, expected right parent.");
        }

        /// <summary>
        /// Tries whether after vertex there is a comma, if there is a comma, that means there are more patterns to parse.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns></returns>
        static private Node ParseNewPatternExpr(List<Token> tokens)
        {
            // Checks for comma, after comma next pattern must be
            if (!CheckToken(position, Token.TokenType.Comma, tokens)) return null;
            IncrementPosition();

            MatchDividerNode matchDivider = new MatchDividerNode();


            Node newPattern = ParseVertex(tokens);
            if (newPattern == null) throw new NullReferenceException("MatchParser, expected Vertex after comma.");
            matchDivider.AddNext(newPattern);
            return matchDivider;
        }


        #endregion MATCH
    }
}
