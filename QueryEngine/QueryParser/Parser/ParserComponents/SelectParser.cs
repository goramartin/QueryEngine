using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    // For more info visit a file Parser.cs
    // Contains a select part of the parser.
    internal static partial class Parser
    {
        #region SELECT

        /// <summary>
        /// Parses select query part.
        /// Select is only parsing expressions separated by comma.
        /// Select -> SELECT (*|(SelectPrintTerm (, SelectPrintTerm)*)
        /// SelectPrintTerm -> Expression
        /// </summary>
        /// <param name="tokens"> Token list to parse. </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Tree representation of a SELECT query part. </returns>
        static public SelectNode ParseSelect(ref int position, List<Token> tokens)
        {
            SelectNode selectNode = new SelectNode();

            // Parsing Select always starts at position 0.
            if (position > 0 || tokens[position].type != Token.TokenType.Select)
                throw new ArgumentException("SelectParser, could not find a Select token, or position is not set at 0.");
            else
            {
                position++;
                Node node = ParseVarExprForSelect(ref position, tokens);
                if (node == null) throw new NullReferenceException("SelectParser, failed to parse Select Expresion.");
                selectNode.AddNext(node);
            }

            return selectNode;
        }


        /// <summary>
        /// Parses list of variables that is Name.Prop, Name2, *, Name3.Prop3 
        /// There can be either only * or variable references.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <param name="position"> Position of a token. </param>
        /// <returns> Chain of variable nodes </returns>
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
        /// Parses select print term node.
        /// Expecting: expression, expression
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        ///  <param name="position"> Position of a token. </param>
        /// <returns> Chain of variable nodes. </returns>
        static private Node ParseSelectPrintTerm(ref int position, List<Token> tokens)
        {
            SelectPrintTermNode selectPrintTermNode = new SelectPrintTermNode();

            var expression = ParseExpressionNode(ref position, tokens);
            if (expression == null) throw new NullReferenceException($"SelectParser, expected expression.");
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
        /// Parses next select print term node.
        /// </summary>
        /// <param name="tokens"> Tokens to parse. </param>
        ///  <param name="position"> Position of a token. </param>
        /// <returns> Chain of print term nodes. </returns>
        static private Node ParseNextSelectPrintNode(ref int position, List<Token> tokens)
        {
            Node nextSelectPrintTermNode = ParseSelectPrintTerm(ref position, tokens);
            if (nextSelectPrintTermNode == null)
                throw new NullReferenceException("SelectParser, expected Indentifier after a comma.");
            else return nextSelectPrintTermNode;
        }

        #endregion SELECT

    }
}
