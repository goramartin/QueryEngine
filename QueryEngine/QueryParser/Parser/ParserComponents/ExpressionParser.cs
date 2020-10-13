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
        /// <param name="position"> Position of a token. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseExpressionNode(ref int position, List<Token> tokens)
        {
            ExpressionNode expressionNode = new ExpressionNode();

            // Expecting successful parse otherwise it would throw inside.
            expressionNode.AddExpression(ParseExpression(ref position, tokens));

            // AS Label 
            if (CheckToken(position, Token.TokenType.AsLabel, tokens))
            {
                position++;
                // Label
                expressionNode.AddLabel(ParseReferenceName(ref position, tokens));
                position++;
            }

            return expressionNode;
        }


        /// <summary>
        /// Prases expression, so far parses only variable reference because there is no other value expression or operators.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Non empty variable node. </returns>
        static private Node ParseExpression(ref int position, List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            // Must parse a variable name.
            variableNode.AddName(ParseReferenceName(ref position, tokens));

            position++;
            // Case of property name .PropName , if there is dot, there must follow identifier.
            if ((CheckToken(position, Token.TokenType.Dot, tokens)))
            {
                position++;
                variableNode.AddProperty(ParseReferenceName(ref position, tokens));
                position++;
            }

            // Returning the node is never empty, there is always a variable reference name other wise it would throw above.
            return variableNode;
        }

        /// <summary>
        /// Forces parsing of an identifier if failed, it throws.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Identifier node with variable name. </returns>
        static private Node ParseReferenceName(ref int position, List<Token> tokens)
        {
            // Expecting identifier.
            Node ident = ParseIdentifierExrp(ref position, tokens);
            if (ident == null)
                throw new NullReferenceException("ExpressionParser, failed to parse identifier.");
            return ident;
        }

        /// <summary>
        /// Parses Identifier token and creates ident node.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Identifier Node </returns>
        static private Node ParseIdentifierExrp(ref int position, List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Identifier, tokens))
                return new IdentifierNode(tokens[position].strValue);
            else return null;
        }


        #endregion Expression

    }



}
