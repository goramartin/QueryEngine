using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    // For more info visit a file Parser.cs
    // Contains the group by part of the parser.
    internal static partial class Parser
    {
        #region GroupBy

        /// <summary>
        /// Parses group by expression. 
        /// Group by expression consists only of expressions separated by comma.
        /// GroupBy -> GROUP BY GroupByTerm (, GroupByTerm)*
        /// GroupByTerm -> Expression
        /// </summary>
        /// <param name="tokens"> Token list to parse </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Tree representation of a group by or null if the tokens are missing group token on its first position. </returns>
        static public GroupByNode ParseGroupBy(ref int position, List<Token> tokens)
        {
            GroupByNode groupByNode = new GroupByNode();

            // We expect after reading match expr that the position is set on the Group token.
            // GROUP
            if (!CheckToken(position, Token.TokenType.Group, tokens))
                return null;
            else
            {

                position++;
                // BY
                if (!CheckToken(position, Token.TokenType.By, tokens))
                    throw new ArgumentException("GroupByParser, failed to parse GroupBy Expresion. Missing BY token");
                else position++;

                Node node = ParseGroupByTerm(ref position, tokens);
                if (node == null) throw new NullReferenceException("GroupByParser, failed to parse group by Expresion. GrouByTerm node is null.");
                groupByNode.AddNext(node);
                return groupByNode;
            }
        }

        /// <summary>
        /// Parses group by term. 
        /// Expression (, Expression)*
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Group by term node. </returns>
        private static Node ParseGroupByTerm(ref int position, List<Token> tokens)
        {
            GroupByTermNode groupByTermNode = new GroupByTermNode();

            // Expression
            var expression = ParseExpressionNode(ref position, tokens);
            if (expression == null)
                throw new NullReferenceException($"GroupByParser, expected expression.");
            else groupByTermNode.AddExpression(expression);

            // Comma signals another order term.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                position++;
                groupByTermNode.AddNext(ParseOrderTerm(ref position, tokens));
            }

            return groupByTermNode;
        }

        #endregion GroupBy

    }
}
