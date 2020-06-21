
/*! \file
   This file contains definitions of a Parser.
  
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
    internal static class Parser
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

        #region SELECT

        /// <summary>
        /// Parses select query part.
        /// Select is only parsing expressions separated by comma.
        /// Select -> SELECT (*|(SelectPrintTerm (, SelectPrintTerm)*)
        /// SelectPrintTerm -> Expression
        /// </summary>
        /// <param name="tokens"> Token list to parse </param>
        /// <returns> Tree representation of a SELECT query part. </returns>
        static public SelectNode ParseSelect(List<Token> tokens)
        {
            SelectNode selectNode = new SelectNode();

            // Parsing Select always starts at position 0.
            if (position > 0 || tokens[position].type != Token.TokenType.Select)
                throw new ArgumentException("SelectParser, could not find a Select token, or position is not set at 0.");
            else
            {
                IncrementPosition();
                Node node = ParseVarExprForSelect(tokens);
                if (node == null) throw new NullReferenceException("SelectParser, cailed to parse Select Expresion.");
                selectNode.AddNext(node);
            }

            return selectNode;
        }


        /// <summary>
        /// Parses list of variables that is Name.Prop, Name2, *, Name3.Prop3 
        /// There can be either only * or variable references.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Chain of variable nodes </returns>
        static private Node ParseVarExprForSelect(List<Token> tokens)
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
                // SelectPrintTerm
                return ParseSelectPrintTerm(tokens);
            }
        }

        /// <summary>
        /// Parses select print term node.
        /// Expecting: expression, expression
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseSelectPrintTerm(List<Token> tokens)
        {
            SelectPrintTermNode selectPrintTermNode = new SelectPrintTermNode();

            var expression = ParseExpressionNode(tokens);
            if (expression == null) throw new NullReferenceException($"SelectParser, expected expression.");
            else selectPrintTermNode.AddExpression(expression);

            // Comma signals there is another expression node, next expression must follow.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                IncrementPosition();
                selectPrintTermNode.AddNext(ParseNextSelectPrintNode(tokens));
            }
            return selectPrintTermNode;
        }

        /// <summary>
        /// Parses next select print term node.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Chain of print term nodes. </returns>
        static private Node ParseNextSelectPrintNode(List<Token> tokens)
        {
            Node nextSelectPrintTermNode = ParseSelectPrintTerm(tokens);
            if (nextSelectPrintTermNode == null)
                throw new NullReferenceException("SelectParser, expected Indentifier after comma.");
            else return nextSelectPrintTermNode;
        }

        #endregion SELECT

        #region MATCH

        /// <summary>
        /// Parsing Match expression, chains of vertex -> edge -> vertex expressions.
        /// Match -> MATCH MatchTerm (, MatchTerm)*
        /// MatchTerm -> Vertex (Edge Vertex)*
        /// Vertex -> (MatchVariable)
        /// Edge -> (EmptyAnyEdge|EmptyOutEdge|EmptyInEdge|AnyEdge|InEdge|OutEdge) 
        /// EmptyAnyEdge -> -
        /// EmptyOutEdge -> <-
        /// EmptyInEdge -> ->
        /// AnyEdge -> -[MatchVariable]-
        /// InEdge -> <-[MatchVariable]-
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
                CheckToken(position + 1, Token.TokenType.Greater, tokens)  &&
                CheckToken(position +2, Token.TokenType.LeftParen, tokens)) return true;
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
            if (CheckToken(position, Token.TokenType.Dash, tokens)){
                IncrementPosition();
                edgeNode = new AnyEdgeNode();
                if (CheckToken(position, Token.TokenType.Greater, tokens)){
                    IncrementPosition();
                    edgeNode = new OutEdgeNode();
                }
            } else throw new ArgumentException($"EdgeParser, expected ending of edge.");

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

        #region ORDERBY


        /// <summary>
        /// Parses order by expression. 
        /// Order by expression consists only of expression optionally followed by ASC or DESC token separated by comma.
        /// OrderBy -> ORDER BY OrderTerm (, OrderTerm)*
        /// OrderTerm -> Expression (ASC|DESC)?
        /// </summary>
        /// <param name="tokens"> Token list to parse </param>
        /// <returns> Tree representation of a order by or null if the tokens are missing order token on its first position. </returns>
        static public OrderByNode ParseOrderBy(List<Token> tokens)
        {
            OrderByNode orderByNode = new OrderByNode();

            // We expect after reading Select expr that the position is set on the Order token.
            // ORDER
            if (!CheckToken(position, Token.TokenType.Order, tokens))
                return null;
            else
            {

                IncrementPosition();
                // BY
                if (!CheckToken(position, Token.TokenType.By, tokens))
                    throw new ArgumentException("OrderByParser, failed to parse OrderBy Expresion. Missing BY token");
                else IncrementPosition();
                

                Node node = ParseOrderTerm(tokens);
                if (node == null) throw new NullReferenceException("OrderByParser, failed to parse order by Expresion. OrderByTerm node is null.");
                orderByNode.AddNext(node);
                return orderByNode;
            }
        }

        /// <summary>
        /// Parses order term. 
        /// Expression (ASC|DESC)?, (Expression (ASC|DESC)?)*
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Order term node. </returns>
        private static Node ParseOrderTerm(List<Token> tokens)
        {
            OrderTermNode orderTermNode = new OrderTermNode();

            // Expression
            var expression = ParseExpressionNode(tokens);
            if (expression == null) 
                throw new NullReferenceException($"OrderByParser, expected expression.");
            else orderTermNode.AddExpression(expression);

            // ASC|DESC
            orderTermNode.SetIsAscending(ParseAscDesc(tokens));

            // Comma singnals another order term.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                IncrementPosition();
                orderTermNode.AddNext(ParseOrderTerm(tokens));
            }

            return orderTermNode;
        }

        /// <summary>
        /// Parses order of an order term.
        /// (ASC|DESC)?
        /// If missing, it is implicitly set to ascending order.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> True for ascending otherwise false. </returns>
        private static bool ParseAscDesc(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Asc, tokens))
            {
                IncrementPosition();
                return true;
            }
            else if (CheckToken(position, Token.TokenType.Desc, tokens))
            {
                IncrementPosition();
                return false;
            }
            else return true;
        }

        #endregion ORDERBY

        #region Expression

        /// <summary>
        /// Parsing of reference variables in select expression: var.PropName AS Label
        /// Expression -> VariableNameReference(.VariablePropertyReference)? AS Label
        /// Label -> IDENTIFIER
        /// VariableNameReference -> IDENTIFIER
        /// VariablePropertyReference -> IDENTIFIER
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseExpressionNode(List<Token> tokens)
        {
            ExpressionNode expressionNode = new ExpressionNode();

            // Expecting successful parse otherwise it would throw inside.
            expressionNode.AddExpression(ParseExpression(tokens));

            // AS Label 
            if (CheckToken(position, Token.TokenType.AsLabel, tokens))
            {
                IncrementPosition();
                // Label
                expressionNode.AddLabel(ParseReferenceName(tokens));
                IncrementPosition();
            }

            return expressionNode;
        }


        /// <summary>
        /// Prases expression, so far parses only variable reference because there is no other value expression or operators.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Non empty variable node. </returns>
        static private Node ParseExpression(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();
            
            // Must parse a variable name.
            variableNode.AddName(ParseReferenceName(tokens));

            IncrementPosition();
            // Case of property name .PropName , if there is dot, there must follow identifier.
            if ((CheckToken(position, Token.TokenType.Dot, tokens)))
            {
                IncrementPosition();
                variableNode.AddProperty(ParseReferenceName(tokens));
                IncrementPosition();
            }

            // Returning the node is never empty, there is always a variable reference name other wise it would throw above.
            return variableNode;
        }

        /// <summary>
        /// Forces parsing of an identifier if failed, it throws.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <returns> Identifier node with variable name. </returns>
        static private Node ParseReferenceName(List<Token> tokens)
        {
            // Expecting identifier.
            Node ident = ParseIdentifierExrp(tokens);
            if (ident == null)
                throw new NullReferenceException("ExpressionParser, failed to parse identifier.");
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


        #endregion Expression
    }

}

