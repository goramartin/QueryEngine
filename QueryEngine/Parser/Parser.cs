
/**
 *  This file contains definitions of Tokens, Tokenizers and Parser.
 * 
 * Parsing is done via Deep descend parsing (Top to bottom).
 * The whole query expression forms a single tree. Each parser method (ParseSelectExpr, ParseMatchExpr...)
 * parses only the part corresponding to the query word and leaves the internal position of the next parsed token
 * to the next token after the last token parsed by methods above.
 * 
 * Visitors then create structures that are used to create query objects.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{


    /// <summary>
    /// Represents single token when parsing. Token type is a type of the token.
    /// </summary>
    struct Token
    {
        public enum TokenType
        {
            Match, Select,

            Asterix, Dot, DoubleDot, Comma, Dash, Less, Greater, LeftParen, RightParen, LeftBrace, RightBrace,
            Identifier,
        }
        public readonly string strValue;
        public TokenType type;

        public Token(string value, TokenType type)
        {
            this.strValue = value;
            this.type = type;
        }
    }

    /// <summary>
    /// Class takes console input and creates tokens based on their string representation.
    /// </summary>
    static class Tokenizer
    {
        // Dict of possible tokens.
        static Dictionary<string, Token.TokenType> tokenTypes;
        // Character ending query.
        static char EndOfQueryCharacter = ';';
        static Tokenizer()
        {
            tokenTypes = new Dictionary<string, Token.TokenType>();
            InitialiseRegistry();
        }

        /// <summary>
        /// Reads input char by char and parses keywords and creates tokens based on the keywords.
        /// </summary>
        /// <param name="reader"> Console reader </param>
        /// <returns> List of parsed tokens </returns>
        public static List<Token> Tokenize(TextReader reader)
        {
            // Result
            List<Token> tokens = new List<Token>();
            int ch = 0;


            while (true)
            {
                ch = reader.Read();
                if (ch == EndOfQueryCharacter) break;

                // Is one symbol token? Case when one character is token.
                // This token does not have a string value only logical value.
                if (tokenTypes.TryGetValue(((char)ch).ToString(), out Token.TokenType token))
                {
                    tokens.Add(new Token(null, token));
                }
                // Skip reading whitespace characters
                else if (char.IsWhiteSpace((char)ch))
                {
                    continue;
                }
                // If the character is a normal letter, we parse the whole consecutive word.
                else if (Char.IsLetter((char)ch))
                {
                    // Get identifier value.
                    string ident = GetIdentifier((char)ch, reader);

                    // Try whether it is a Query word.
                    // Query word is a SELECT, MATCH ...
                    Token.TokenType tok = default;
                    if (tokenTypes.TryGetValue(ident, out tok))
                    {
                        tokens.Add(new Token(null, tok));
                    }
                    //Else it is identifier that has got a string value.
                    else { tokens.Add(new Token(ident, Token.TokenType.Identifier)); }
                }
                else throw new ArgumentException($"{(char)ch} Found character that could not be parsed. Tokenizer.");
            }

            return tokens;
        }


        /// <summary>
        /// Reads single word from an input.
        /// </summary>
        /// <param name="ch"> First consumed character. </param>
        /// <param name="reader"> Console reader </param>
        /// <returns> Word from input starting with character from parameters. </returns>
        private static string GetIdentifier(char ch, TextReader reader)
        {
            string strValue = "";
            strValue += ch;

            while (true)
            {
                int peekedChar = reader.Peek();
                if (char.IsLetter((char)peekedChar))
                {
                    strValue += (char)peekedChar;
                    reader.Read();
                }
                else { break; }
            }

            return strValue;

        }


        /// <summary>
        /// Inserts token with its input representaion into token registry.
        /// </summary>
        /// <param name="str"> String representation in input.</param>
        /// <param name="type"> Token type </param>
        private static void RegisterToken(string str, Token.TokenType type)
        {

            if (tokenTypes.ContainsKey(str))
                throw new ArgumentException("TokenRegistry: Token Type already registered.");

            tokenTypes.Add(str, type);
        }

        /// <summary>
        /// Inserts all possible tokens with their assiciative string values in input.
        /// </summary>
        private static void InitialiseRegistry()
        {
            RegisterToken("*", Token.TokenType.Asterix);
            RegisterToken(",", Token.TokenType.Comma);
            RegisterToken(".", Token.TokenType.Dot);
            RegisterToken(":", Token.TokenType.DoubleDot);
            RegisterToken("-", Token.TokenType.Dash);
            RegisterToken(">", Token.TokenType.Greater);
            RegisterToken("<", Token.TokenType.Less);
            RegisterToken("[", Token.TokenType.LeftBrace);
            RegisterToken("]", Token.TokenType.RightBrace);
            RegisterToken("(", Token.TokenType.LeftParen);
            RegisterToken(")", Token.TokenType.RightParen);
            RegisterToken("MATCH", Token.TokenType.Match);
            RegisterToken("SELECT", Token.TokenType.Select);
            RegisterToken("match", Token.TokenType.Match);
            RegisterToken("select", Token.TokenType.Select);
        }

    }



    /// <summary>
    /// Creates query tree from tokens. Using deep descend parsing method. Top -> Bottom method.
    /// </summary>
    static class Parser
    {
        // Position in token list.
        static int position;
        static Parser() { position = 0; }


        // Methods to change value of position.
        static public int GetPosition() { return position; }
        static public void ResetPosition() { position = 0; }
        static private void IncrementPosition() { position++; }
        static private void IncrementPositionBy(int p) { position += p; }


        /**
         * Each query words is parsed separately.
         * Parsing should always start with parsing select and match
         * since they are compulsory to use.
         * Parsing Select always starts at position 0.
         * When finished parsing query word, the position is set on the next token.
         */



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
                Node node = ParseVariableExpr(tokens);
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



        /// <summary>
        /// Parses list of variables that is Name.Prop, Name2, *, Name3.Prop3 
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Chain of variable nodes </returns>
        static private Node ParseVariableExpr(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            // (*)
            if (CheckToken(position, Token.TokenType.Asterix, tokens))
            {
                variableNode.AddName(new IdentifierNode("*"));
                IncrementPosition();
            }
            else
            {
                //Expecting identifier.
                Node name = ParseIdentifierExrp(tokens);
                if (name == null) return null;
                else variableNode.AddName(name);

                IncrementPosition();
                //Case of property name .PropName , if there is dot, there must follow identifier.
                if ((CheckToken(position, Token.TokenType.Dot, tokens)))
                {
                    IncrementPosition();
                    Node identifierNode = ParseIdentifierExrp(tokens);
                    if (identifierNode == null) throw new ArgumentException("VariableParser, exprected Indentifier after dot.");
                    else variableNode.AddProperty(identifierNode);
                    IncrementPosition();
                }
            }

            //Comma signals there is another variable, next variablenode must follow.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                IncrementPosition();
                Node nextVariableNode = ParseVariableExpr(tokens);
                if (nextVariableNode == null) throw new ArgumentException("VariableParser, exprected Indentifier after comma.");
                else variableNode.AddNext(nextVariableNode);
            }
            return variableNode;
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


        /**
         * Parsing Match expression is done with combination of parsing variables enclosed in 
         * vertex or edge.
         */


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

            // (
            if (CheckToken(position, Token.TokenType.LeftParen, tokens)) IncrementPosition();
            else return null;

            //Parse Values of the variable.
            Node variableNode = ParseVarForMatchExpr(tokens);
            vertexNode.AddVariable(variableNode);

            // )
            if (CheckToken(position, Token.TokenType.RightParen, tokens)) IncrementPosition();
            else return null;

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
        /// Parses edge expression altogether with enclosed variable  -[...]- / <-[...]- / -[...]->
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Edge node </returns>
        static private Node ParseEdgeExpr(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();

            // Check whether it is an anonymous edge
            Node anonymousEdge = ParseAnonymousEdge(tokens);

            //Empty edge node or normal edges
            if (anonymousEdge != null) edgeNode = (EdgeNode)anonymousEdge;
            else
            {
                Node normalEdge = ParseEdge(tokens);
                if (normalEdge == null) return null;
                else edgeNode = (EdgeNode)normalEdge;
            }

            //Next must be vertex.
            Node vertexNode = ParseVertexExpr(tokens);
            if (vertexNode != null) edgeNode.AddNext(vertexNode);
            else throw new ArgumentException("ParseEdge, expected vertex.");

            return edgeNode;
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


        /// <summary>
        /// Check for anonymous edge.
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Empty edge node </returns>
        static private Node ParseAnonymousEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();

            if (CheckOutEdgeHead(position, tokens))
            {
                edgeNode.SetEdgeType(EdgeType.OutEdge);
                IncrementPositionBy(2);
            }
            else if (CheckAnyEdgeHead(position, tokens) &&
                    (!CheckToken(position + 1, Token.TokenType.LeftBrace, tokens)))
            {
                edgeNode.SetEdgeType(EdgeType.AnyEdge);
                IncrementPosition();
            }
            else if (CheckInEdgeHead(position, tokens) &&
                    !CheckToken(position + 2, Token.TokenType.LeftBrace, tokens))
            {
                edgeNode.SetEdgeType(EdgeType.InEdge);
                IncrementPositionBy(2);
            }

            if (edgeNode.GetEdgeType() != default(EdgeType)) return edgeNode;
            else return null;

        }

        /// <summary>
        /// Parses non empty edge
        /// </summary>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> Non empty edge node </returns>
        static private Node ParseEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();
            //Define type of edge.  in <...-, out -...>, any -...-
            EdgeType type = DefineEdgeType(tokens);
            if (type == EdgeType.NotEdge) return null;
            else
            {
                edgeNode.SetEdgeType(type);
                if (type == EdgeType.InEdge) IncrementPositionBy(2);
                else IncrementPosition();
            }

            // [
            if (CheckToken(position, Token.TokenType.LeftBrace, tokens)) IncrementPosition();
            else throw new ArgumentException("ParseEdge, expected Leftbrace.");

            //Parse variable of edge.
            Node variableNode = ParseVarForMatchExpr(tokens);
            if (variableNode == null) throw new ArgumentException("ParseEdge, expected variable.");
            else edgeNode.AddVariable(variableNode);

            // ]
            if (CheckToken(position, Token.TokenType.RightBrace, tokens)) IncrementPosition();
            else throw new ArgumentException("ParseEdge, expected rightbrace.");


            //Skip end character of edge.  ->,-
            if (type == EdgeType.OutEdge) IncrementPositionBy(2);
            else IncrementPosition();

            return edgeNode;
        }

        /// <summary>
        /// Find the type of parsed edge 
        /// </summary>
        /// <param name="tokens">Tokens to parse </param>
        /// <returns> type of edge </returns>
        static private EdgeType DefineEdgeType(List<Token> tokens)
        {
            //The order of checks matter. We first must refute out edge,
            //else there could be any edge instead of out edge.
            //Out edge -[..]->
            if (PredictEdgeTypeOut(tokens))
                return EdgeType.OutEdge;
            //In edge <-[..]-
            else if (PredictEdgeTypeIn(tokens))
                return EdgeType.InEdge;
            //Any edge -[..]-
            else if (PredictEgeTypeAny(tokens))
                return EdgeType.AnyEdge;
            else return EdgeType.NotEdge;
        }


        /// <summary>
        /// Check if the parsed edge is of any type 
        /// </summary>
        /// <param name="tokens">Tokens to parse</param>
        /// <returns> True on match </returns>
        static private bool PredictEgeTypeAny(List<Token> tokens)
        {
            // -[e]-
            int pOne = position + 4;
            // -[:prop]-
            int pTwo = position + 5;
            // -[e:x]-
            int pThree = position + 6;
            if (CheckAnyEdgeHead(position, tokens) && CheckDashForward(pOne, pTwo, pThree, tokens))

                return true;
            else return false;
        }


        /// <summary>
        /// Check if the parsed edge is of in type 
        /// </summary>
        /// <param name="tokens">Tokens to parse</param>
        /// <returns> True on match </returns>
        static private bool PredictEdgeTypeIn(List<Token> tokens)
        {
            // <-[e]-
            int pOne = position + 5;
            // <-[:prop]-
            int pTwo = position + 6;
            // <-[e:x]-
            int pThree = position + 7;
            if (CheckInEdgeHead(position, tokens) && CheckDashForward(pOne, pTwo, pThree, tokens))
                return true;
            else return false;
        }

        /// <summary>
        /// Check if the parsed edge is of out type 
        /// </summary>
        /// <param name="tokens">Tokens to parse</param>
        /// <returns> True on match </returns>
        static private bool PredictEdgeTypeOut(List<Token> tokens)
        {
            // -[e]->
            int pOne = position + 4;
            // -[:prop]->
            int pTwo = position + 5;
            // -[e:x]->
            int pThree = position + 6;
            if (CheckAnyEdgeHead(position, tokens) && CheckOutEdgeHeadForward(pOne, pTwo, pThree, tokens))
                return true;
            else return false;
        }

        /// <summary>
        /// Check if on given positions there is a dash
        /// </summary>
        /// <returns> True on dash -[...]"-" match</returns>
        static private bool CheckDashForward(int first, int second, int third, List<Token> tokens)
        {
            if (CheckToken(first, Token.TokenType.Dash, tokens) ||
             CheckToken(second, Token.TokenType.Dash, tokens) ||
             CheckToken(third, Token.TokenType.Dash, tokens)) return true;
            else return false;
        }



        /// <summary>
        /// Checks if the token is head of any edge
        /// </summary>
        /// <param name="p"> position of token </param>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> True on match with -[...]"->"  token </returns>
        static private bool CheckOutEdgeHeadForward(int first, int second, int third, List<Token> tokens)
        {
            if (CheckOutEdgeHead(first, tokens) ||
                CheckOutEdgeHead(second, tokens) ||
                (CheckOutEdgeHead(third, tokens)))
                return true;
            else return false;
        }


        /// <summary>
        /// Checks if the token is head of in edge
        /// </summary>
        /// <param name="p"> position of token </param>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> True on match with <- token </returns>
        static private bool CheckInEdgeHead(int p, List<Token> tokens)
        {
            if (CheckToken(p, Token.TokenType.Less, tokens) &&
                    CheckToken(p + 1, Token.TokenType.Dash, tokens))
                return true;
            else return false;
        }



        /// <summary>
        /// Checks if the token is head of out  edge
        /// </summary>
        /// <param name="p"> position of token </param>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> True on match with -> token </returns>
        static private bool CheckOutEdgeHead(int p, List<Token> tokens)
        {
            if (CheckToken(p, Token.TokenType.Dash, tokens) &&
                     (CheckToken(p + 1, Token.TokenType.Greater, tokens)))
                return true;
            else return false;
        }


        /// <summary>
        /// Checks if the token is head of any edge
        /// </summary>
        /// <param name="p"> position of token </param>
        /// <param name="tokens"> Tokens to parse </param>
        /// <returns> True on match with dash token </returns>
        static private bool CheckAnyEdgeHead(int p, List<Token> tokens)
        {
            if (CheckToken(p, Token.TokenType.Dash, tokens))
                return true;
            else return false;
        }



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

