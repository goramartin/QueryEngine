using System.Collections.Generic;

namespace QueryEngine
{
    // For more info visit a file Parser.cs
    // Contains the group by part of the parser.
    internal static partial class Parser
    {
        #region GroupBy

        /// <summary>
        /// Parses a group by expression. 
        /// Group by expression consists only of expressions separated by comma.
        /// GroupBy -> GROUP BY GroupByTerm (, GroupByTerm)*
        /// GroupByTerm -> Expression
        /// </summary>
        /// <returns> A tree representation of a group by or null if the tokens are missing group token on its first position. </returns>
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
                   ThrowError("Group by parser", "Expected BY token.", position, tokens);
                else position++;

                Node node = ParseGroupByTerm(ref position, tokens);
                if (node == null) ThrowError("Group by parser", "Failed to parse group by expresion. Expected group by term.", position, tokens);
                groupByNode.AddNext(node);
                return groupByNode;
            }
        }

        /// <summary>
        /// Parses a group by term. 
        /// Expression (, Expression)*
        /// </summary>
        /// <returns> A group by term node. </returns>
        private static Node ParseGroupByTerm(ref int position, List<Token> tokens)
        {
            GroupByTermNode groupByTermNode = new GroupByTermNode();

            // Expression
            var expression = ParseExpressionNode(ref position, tokens);
            if (expression == null)
                ThrowError("Group by parser", "Expected expression.", position, tokens);
            else groupByTermNode.AddExpression(expression);

            // Comma signals another group  term.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                position++;
                groupByTermNode.AddNext(ParseGroupByTerm(ref position, tokens));
            }

            return groupByTermNode;
        }

        #endregion GroupBy
    }
}
