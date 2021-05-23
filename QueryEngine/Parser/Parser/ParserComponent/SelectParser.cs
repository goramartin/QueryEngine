using System.Collections.Generic;

namespace QueryEngine
{
    // For more info visit a file Parser.cs
    // Contains a select part of the parser.
    internal static partial class Parser
    {
        #region SELECT

        /// <summary>
        /// Parses a select query part.
        /// Select is only parsing expressions separated by comma.
        /// Select -> SELECT (*|(SelectPrintTerm (, SelectPrintTerm)*)
        /// SelectPrintTerm -> Expression
        /// </summary>
        /// <returns> A tree representation of a SELECT query part. </returns>
        static public SelectNode ParseSelect(ref int position, List<Token> tokens)
        {
            SelectNode selectNode = new SelectNode();

            // Parsing Select always starts at position 0.
            if (position > 0 || tokens[position].type != Token.TokenType.Select)
                ThrowError("Select parser", "Failed to find SELECT token.", position, tokens);
            else
            {
                position++;
                Node node = ParseVarExprForSelect(ref position, tokens);
                if (node == null) ThrowError("Select parser", "Expected Select print term.", position, tokens);
                selectNode.AddNext(node);
            }

            return selectNode;
        }


        /// <summary>
        /// Parses a List of variables - Name.Prop, Name2, *, Name3.Prop3 
        /// There can be either only * or variable references.
        /// </summary>
        /// <returns> A chain of variable nodes </returns>
        static private Node ParseVarExprForSelect(ref int position, List<Token> tokens)
        {
            VariableNode variableNode = null;

            // (*)
            if (CheckToken(position, Token.TokenType.Asterix, tokens))
            {
                variableNode = new VariableNode();
                variableNode.AddName(new IdentifierNode("*"));
                position++;
                return variableNode;
            } 
            else
            {
                // SelectPrintTerm
                return ParseSelectPrintTerm(ref position, tokens);
            }
        }

        /// <summary>
        /// Parses a select print term node.
        /// Expecting: expression, expression
        /// </summary>
        /// <returns> A chain of variable nodes. </returns>
        static private Node ParseSelectPrintTerm(ref int position, List<Token> tokens)
        {
            SelectPrintTermNode selectPrintTermNode = new SelectPrintTermNode();

            var expression = ParseExpressionNode(ref position, tokens);
            if (expression == null) ThrowError("Select parser", "Expected expression.", position, tokens);
            else selectPrintTermNode.AddExpression(expression);

            // Comma signals there is another expression node, next expression must follow.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                position++;
                selectPrintTermNode.AddNext(ParseNextSelectPrintNode(ref position, tokens));
            }
            return selectPrintTermNode;
        }

        /// <summary>
        /// Parses the next select print term node.
        /// </summary>
        /// <returns> A chain of print term nodes. </returns>
        static private Node ParseNextSelectPrintNode(ref int position, List<Token> tokens)
        {
            Node nextSelectPrintTermNode = ParseSelectPrintTerm(ref position, tokens);
            if (nextSelectPrintTermNode == null)
            {
                ThrowError("Select parser", "Expected another Select print term after comma.", position, tokens);
                return null; // Cannot get here.
            }
            else return nextSelectPrintTermNode;
        }

        #endregion SELECT
    }
}
