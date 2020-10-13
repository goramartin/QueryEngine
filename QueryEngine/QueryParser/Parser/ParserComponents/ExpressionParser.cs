using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

   // For more infor visit a file Parser.cs
   // Contains the expression part of the parser.
   internal static partial class Parser
   {
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
