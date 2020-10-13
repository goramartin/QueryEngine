using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    // For more info visit a file Parser.cs
    // Contains the order by part of the parser.
    internal static partial class Parser
    {
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
    }
}
