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
        /// Expression -> ExpressionTerm AS Label
        /// ExpressionTerm -> AggregateFunc|VarReference
        /// AggregateFunc -> IDENTIFIER \( VarReference \)
        /// VarReference -> ReferenceName(.ReferenceName)?
        /// Label -> IDENTIFIER
        /// ReferenceName -> IDENTIFIER
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseExpressionNode(ref int position, List<Token> tokens)
        {
            ExpressionNode expressionNode = new ExpressionNode();

            // Expecting successful parse otherwise it would throw inside.
            expressionNode.AddExpression(ParseExpressionTerm(ref position, tokens));

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
        /// ExpressionTerm -> AggregateFunc|VarReference
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Non empty aggregate node or variable reference node. </returns>
        static private Node ParseExpressionTerm(ref int position, List<Token> tokens)
        {
            Node aggFunc = ParseAggregateFunc(ref position, tokens);
            if (aggFunc != null) return aggFunc;
            else return ParseVarReference(ref position, tokens);
        }


        /// <summary>
        /// AggregateFunc -> IDENTIFIER \( VarReference \)
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Non empty aggregate node. </returns>
        static private Node ParseAggregateFunc(ref int position, List<Token> tokens)
        {
            // FuncName (
            if (!((CheckToken(position, Token.TokenType.Identifier, tokens)) &&
                    (CheckToken(position + 1, Token.TokenType.LeftParen, tokens)))) return null;
            else
            {
                AggregateFuncNode aggregate = new AggregateFuncNode();
                
                // Save the name of the function.
                aggregate.func = tokens[position].strValue;
                // It must inc by 2 because it was +1 moves it to left parent and another +1 moves it to next token.
                position += 2;
                aggregate.next = ParseVarReference(ref position, tokens);

                // ) // also moves position on success
                CheckRightParen(ref position, tokens);

                return aggregate;
            }
        }

        /// <summary>
        /// Parses only variable reference because there is no other value expression or operators.
        /// VarReference -> ReferenceName(.ReferenceName)?
        /// ReferenceName -> IDENTIFIER
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Non empty variable node. </returns>
        static private Node ParseVarReference(ref int position, List<Token> tokens)
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
