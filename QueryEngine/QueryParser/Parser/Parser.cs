
/*! \file
   This file contains definitions of a Parser.
  
  Parsing is done via Deep descend parsing (Top to bottom).
  The whole query expression forms a single tree. Each parser method (ParseSelectExpr, ParseMatchExpr...)
  parses only the part corresponding to the query word and leaves the internal position of the next parsed token
  to the next token after the last token parsed by methods above.
  
  Visitors then create structures that are used to create query objects.
  
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
    /// When finished parsing query word, the position is set on the next token.
    /// </summary>
    static class Parser
    {
        // Position in token list.
        static private int position;
        static Parser() { position = 0; }


        // Methods to change value of position.
        static public int GetPosition() { return position; }
        static public void ResetPosition() { position = 0; }
        static private void IncrementPosition() { position++; }
        static private void IncrementPositionBy(int p) { position += p; }

        /// <summary>
        /// Parses select query part.
        /// Select is only parsing variables, that is XXX.YYY inputs separated by comma.
        /// </summary>
        /// <param name="tokens"> Token list to parse </param>
        /// <returns> Tree representation of a SELECT query part. </returns>
        static public SelectNode ParseSelectExpr(List<Token> tokens)
        {
            SelectNode selectNode = new SelectNode();

            // Parsing Select always starts at position 0.
            if (position > 0 || tokens[position].type != Token.TokenType.Select)
                throw new ArgumentException("SelectParser, Could not find a Select token, or position is not set at 0.");
            else
            {
                IncrementPosition();
                Node node = ParseVarExprForSelectExpr(tokens);
                if (node == null) throw new ArgumentException("Failed to parse Select Expresion.");
                selectNode.AddNext(node);
            }

            return selectNode;
        }
        /// <summary>
        /// Parsing Match expression, chains of vertex -> edge -> vertex expressions.
        /// </summary>
        /// <param name="tokens"> Token list to parse </param>
        /// <returns> Tree representation of Match expression </returns>
        static public MatchNode ParseMatchExpr(List<Token> tokens)
        {
            MatchNode matchNode = new MatchNode();

            // We expect after reading Select expr that the position is set on the Match token.
            if (!CheckToken(position, Token.TokenType.Match, tokens))
                throw new ArgumentException("SelectParser, position is not set at Match Token.");
            else
            {
                IncrementPosition();
                Node node = ParseVertexExpr(tokens);
                if (node == null) throw new ArgumentException("Failed to parse Match Expresion.");
                matchNode.AddNext(node);
            }
            return matchNode;

        }

        #region SELECT

        /// <summary>
        /// Parses list of variables that is Name.Prop, Name2, *, Name3.Prop3 
        /// There can be either only * or variable references.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Chain of variable nodes </returns>
        static private Node ParseVarExprForSelectExpr(List<Token> tokens)
        {
            VariableNode variableNode = null;

            // (*)
            if (CheckToken(position, Token.TokenType.Asterix, tokens))
            {
                variableNode = new VariableNode();
                variableNode.AddName(new IdentifierNode("*"));
                IncrementPosition();
                return variableNode;
            }
            else
            {
                return ParseVarExprForSelectExprNoAsterix(tokens);
            }
        }

        /// <summary>
        /// Parsing of reference variables in select expression: var.PropName AS Label
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseVarExprForSelectExprNoAsterix(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();
            variableNode.AddName(ParseIdentVariableForSelectExpr(tokens));

            IncrementPosition();
            // Case of property name .PropName , if there is dot, there must follow identifier.
            // If there is a prop name we can use labels "AS label"
            if ((CheckToken(position, Token.TokenType.Dot, tokens)))
            {
                IncrementPosition();
                variableNode.AddProperty(ParseIdentVariableForSelectExpr(tokens));
                IncrementPosition();

                if (CheckToken(position, Token.TokenType.AsLabel, tokens))
                {
                    IncrementPosition();
                    variableNode.AddLabel(ParseIdentVariableForSelectExpr(tokens));
                    IncrementPosition();
                }
            }

            // Comma signals there is another variable, next variablenode must follow.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                IncrementPosition();
                variableNode.AddNext(ParseNextVariableForSelectExprNoAsterix(tokens));
            }
            return variableNode;
        }

        /// <summary>
        /// Parses next variable node.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseNextVariableForSelectExprNoAsterix(List<Token> tokens)
        {
            Node nextVariableNode = ParseVarExprForSelectExprNoAsterix(tokens);
            if (nextVariableNode == null)
                throw new ArgumentException("VariableParser, exprected Indentifier after comma.");
            else return nextVariableNode;
        }

        /// <summary>
        /// Parses name of variable (before .)
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Identifier node with variable name. </returns>
        static private Node ParseIdentVariableForSelectExpr(List<Token> tokens)
        {
            // Expecting identifier.
            Node ident = ParseIdentifierExrp(tokens);
            if (ident == null)
                throw new ArgumentNullException("VariableParser, failed to parse ident.");
            return ident;
        }

        /// <summary>
        /// Parses Identifier token and creates ident node.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Identifier Node </returns>
        static private Node ParseIdentifierExrp(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Identifier, tokens))
                return new IdentifierNode(tokens[position].strValue);
            else return null;
        }

        #endregion SELECT

        #region MATCH

        /// <summary>
        /// Parses variable enclosed in vertex or edge.
        /// Expects  Name:Type / Name / :Type / (nothing)
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Variable node </returns>
        static private Node ParseVarForMatchExpr(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            //Expecting identifier, name of variable. Can be empty, if so then it is anonymous variable.
            Node name = ParseIdentifierExrp(tokens);
            if (name != null) { IncrementPosition(); }
            variableNode.AddName(name);

            //Check for type of vairiable after :
            if (CheckToken(position, Token.TokenType.DoubleDot, tokens))
            {
                IncrementPosition();
                Node identifierNode = ParseIdentifierExrp(tokens);
                if (identifierNode == null) throw new ArgumentException("VariableForMatchParser, exprected Indentifier after double dot.");
                else variableNode.AddProperty(identifierNode);
                IncrementPosition();
            }

            if (variableNode.IsEmpty()) return null;
            else return variableNode;
        }

        /// <summary>
        /// Parses vertex node, (n) / (n:Type) / () / (:Type)
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Vertex node </returns>
        static private Node ParseVertexExpr(List<Token> tokens)
        {
            VertexNode vertexNode = new VertexNode();

            CheckLeftParen(tokens);
            //Parse Values of the variable.
            Node variableNode = ParseVarForMatchExpr(tokens);
            vertexNode.AddVariable(variableNode);
            CheckRightParen(tokens);

            //Position incremented from leaving function PsrseVariable.
            //Try parse an Edge.
            Node edgeNode = ParseEdgeExpr(tokens);
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
        static private Node ParseEdgeExpr(List<Token> tokens)
        {
            EdgeNode edgeNode = null;

            edgeNode = (EdgeNode)TryParseAnonymousEdge(tokens);
            if (edgeNode == null)
            {
                edgeNode =(EdgeNode)ParseEdgeWithVar(tokens);
                if (edgeNode == null) return null;
                // to do check end 
            }

            Node vertexNode = ParseVertexExpr(tokens);
            if (vertexNode != null) edgeNode.AddNext(vertexNode);
            else throw new ArgumentException("ParseEdge, expected vertex.");

            return edgeNode;

        }
        /// <summary>
        /// Tries to parse anonumous edge type.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Anonymous edge or null. </returns>
        static private Node TryParseAnonymousEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();
            if (CheckAnonymousAnyEdge(tokens))
            {
                edgeNode.SetEdgeType(EdgeType.AnyEdge);
                IncrementPosition();
                return edgeNode;
            }
            else if (CheckAnonymousOutEdge(tokens))
            {
                edgeNode.SetEdgeType(EdgeType.OutEdge);
                IncrementPositionBy(2);
                return edgeNode;
            }
            else if (CheckAnonymousInEdge(tokens))
            {
                edgeNode.SetEdgeType(EdgeType.InEdge);
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
        private static bool CheckAnonymousAnyEdge(List<Token> tokens)
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
        private static bool CheckAnonymousOutEdge(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                CheckToken(position + 1, Token.TokenType.Greater, tokens)  &&
                CheckToken(position +2, Token.TokenType.LeftParen, tokens)) return true;
            else return false;
        }

        /// <summary>
        /// Check tokens for anonymous in edge
        /// </summary>
        /// <param name="tokens">Tokens to parse.</param>
        /// <returns> True on matched pattern.</returns>
        private static bool CheckAnonymousInEdge(List<Token> tokens)
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
        private static Node ParseEdgeWithVar(List<Token> tokens)
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
            EdgeNode edgeNode = new EdgeNode();

            CheckLeftBrace(tokens);           
            edgeNode.AddVariable(ParseVarForMatchExpr(tokens));
            CheckRightBrace(tokens);


            // -> || -
            if (CheckToken(position, Token.TokenType.Dash, tokens)){
                IncrementPosition();
                edgeNode.SetEdgeType(EdgeType.AnyEdge);
                if (CheckToken(position, Token.TokenType.Greater, tokens)){
                    IncrementPosition();
                    edgeNode.SetEdgeType(EdgeType.OutEdge);
                }
            } else throw new ArgumentException($"EdgeParser, expected ending of edge.");

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
            EdgeNode edgeNode = new EdgeNode();
            
            // -
            if (!CheckToken(position, Token.TokenType.Dash, tokens)) 
                throw new ArgumentException($"EdgeParser, expected beginning of edge.");
            else IncrementPosition();

            CheckLeftBrace(tokens);
            edgeNode.AddVariable(ParseVarForMatchExpr(tokens));
            CheckRightBrace(tokens);

            // -
            if (CheckToken(position, Token.TokenType.Dash, tokens))
            {
                IncrementPosition();
                edgeNode.SetEdgeType(EdgeType.InEdge);
            }
            else throw new ArgumentException($"EdgeParser, expected ending of edge.");

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
            else throw new ArgumentException("MatchParser variable, expected left parent.");
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
            else throw new ArgumentException("MatchParser variable, expected right parent.");
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

            MatchDivider matchDivider = new MatchDivider();


            Node newPattern = ParseVertexExpr(tokens);
            if (newPattern == null) throw new ArgumentException("ParseNewPatern, expected new pattern.");
            matchDivider.AddNext(newPattern);
            return matchDivider;
        }


        #endregion MATCH

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

