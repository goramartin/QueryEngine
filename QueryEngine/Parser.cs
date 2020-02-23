
/**
 *  This file contains definitions of Tokens, Tokenizers, Parsers and assiciated classes,
 *  those are... nodes that the Parser uses to create parsing tree and visitors to 
 *  process the parsing tree.
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
            }
            else
            {
                //Expecting identifier.
                Node name = ParseIdentifierExrp(tokens);
                if (name == null) return null;
                else variableNode.AddName(name);
            }

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




    /// <summary>
    /// Parse tree is processed via visitors
    /// </summary>
    /// <typeparam name="T"> Object built after parsing </typeparam>
    interface IVisitor<T>
    {
        T GetResult();
        void Visit(SelectNode node);
        void Visit(MatchNode node);
        void Visit(MatchDivider node);
        void Visit(VertexNode node);
        void Visit(EdgeNode node);
        void Visit(VariableNode node);
        void Visit(IdentifierNode node);
    }


    /// <summary>
    /// Creates list of variable (Name.Prop) to be displayed in Select expr.
    /// </summary>
    class SelectVisitor : IVisitor<List<SelectVariable>>
    {
        List<SelectVariable> result;
        bool addingName; // Whether adding name or property, TRUE for adding name 

        public SelectVisitor()
        {
            result = new List<SelectVariable>();
            addingName = true;
        }

        public List<SelectVariable> GetResult()
        { return this.result; }


        /// <summary>
        /// Starts parsing from select node, does nothing only jumps to next node.
        /// There must be at least one variable to be displyed.
        /// </summary>
        /// <param name="node"> Select node </param>
        public void Visit(SelectNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException("SelectVisitor, failed to parse select expr.");
        }



        /// <summary>
        /// Create new variable and try parse its name and propname.
        /// Name shall never be null. Name is identifier node.
        /// Jump to next variable node.
        /// </summary>
        /// <param name="node"> Variable node </param>
        public void Visit(VariableNode node)
        {
            addingName = true;
            result.Add(new SelectVariable());
            if (node.name == null)
                throw new ArgumentException("SelectVisitor, could not parse variable name.");
            else
            {
                // Jump to identifier node with string value of name 
                node.name.Accept(this);
                addingName = false;

                // If the propname is set, jump to identifier node of property
                if (node.propName != null) node.propName.Accept(this);
            }

            if (node.next == null) return;
            else node.next.Accept(this);

        }

        /// <summary>
        /// Obtains string value of variable or property. 
        /// </summary>
        /// <param name="node"> Identifier node </param>
        public void Visit(IdentifierNode node)
        {
            //If adding name it must be successful, otherwise it is failed parsing.
            //There is always one object in results, count -1 can never undergo limit.
            if (addingName)
            {
                if (!result[result.Count - 1].TrySetName(node.value))
                    throw new ArgumentException("SelectVisitor, could not set name to variable.");
            }
            //If it try assign propname, it also must always be success, 
            //because it could not be assigned before this.
            else
            {
                if (!result[result.Count - 1].TrySetPropName(node.value))
                    throw new ArgumentException("SelectVisitor, could not set propname to variable.");
            }

        }

        //Can never appear.  
        public void Visit(MatchDivider node)
        {
            throw new NotImplementedException();
        }
        public void Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(EdgeNode node)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Creates List of single pattern chains which will form the whole pattern later in MatchQueryObject.
    /// </summary>
    class MatchVisitor : IVisitor<List<ParsedPattern>>
    {
        List<ParsedPattern> result;
        ParsedPattern currentPattern;
        Dictionary<string, Table> vTables;
        Dictionary<string, Table> eTables;
        bool readingName;
        bool readingVertex;

        public MatchVisitor( Dictionary<string, Table> v, Dictionary<string, Table> e)
        {
            this.currentPattern = new ParsedPattern();
            this.result = new List<ParsedPattern>();
            this.vTables = v;
            this.eTables = e;
            this.readingName = true;
            this.readingVertex = true;
        }


        public List<ParsedPattern> GetResult()
        { return this.result; }

        /// <summary>
        /// Jumps to vertex node.
        /// All patterns must have at least one match.
        /// There is always at least one ParsedPattern.
        /// </summary>
        /// <param name="node"> Match node </param>
        public void Visit(MatchNode node)
        {
            //Create new pattern and start its parsing.
            result.Add(currentPattern);
            node.next.Accept(this);

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].GetCount() <= 0)
                    throw new ArgumentException("MatchVisitor, failed to parse match expr.");
            }
        }



        /// <summary>
        /// Processes vertex node, try to jump to variable inside vertex or continue to the edge.
        /// </summary>
        /// <param name="node"> Vertex Node </param>
        public void Visit(VertexNode node)
        {
            this.readingVertex = true;

            ParsedPatternNode vm = new ParsedPatternNode();
            vm.isVertex = true;
            currentPattern.AddParsedPatternNode(vm);

            if (node.variable != null) node.variable.Accept(this);
            if (node.next != null) node.next.Accept(this);
        }

        /// <summary>
        /// Processes Edge node, tries to jump to variable node inside edge or to the next vertex.
        /// </summary>
        /// <param name="node"> Edge node </param>
        public void Visit(EdgeNode node)
        {
            this.readingVertex = false;

            ParsedPatternNode em = new ParsedPatternNode();
            em.edgeType = node.GetEdgeType();
            em.isVertex = false;
            currentPattern.AddParsedPatternNode(em);

            if (node.variable != null) node.variable.Accept(this);
            if (node.next == null)
                throw new ArgumentException("MatchVisitor, missing end vertex from edge.");
            else node.next.Accept(this);

        }

        /// <summary>
        /// Processes variable node.
        /// Always jumps to identifier node where Name and promerty name is processed.
        /// </summary>
        /// <param name="node"> Variable node </param>
        public void Visit(VariableNode node)
        {
            readingName = true;
            //It is not anonnymous field.
            if (node.name != null)
            {
                node.name.Accept(this);
            }
            //It has set type.
            if (node.propName != null)
            {
                readingName = false;
                node.propName.Accept(this);
            }

        }

        /// <summary>
        /// Processes Identifier node.
        /// Either assigns name of variable to last ParsedPatternNode or table pertaining to the node. 
        /// </summary>
        /// <param name="node">Identifier node </param>
        public void Visit(IdentifierNode node)
        {
            ParsedPatternNode n = currentPattern.GetLastPasrsedPatternNode();
           
            if (readingName)
            {
                n.isAnonymous = false;
                n.name = node.value;
            }
            else
            {
                if (readingVertex) ProcessType(node, vTables, n);
                else ProcessType(node, eTables,n);
            }
        }

        /// <summary>
        /// Tries to find table based on indentifier node value and assign it to parsed node.
        /// </summary>
        /// <param name="node"> Identifier node from Visiting indetifier node.</param>
        /// <param name="d"> Dictionary of tables from edges/vertices. </param>
        /// <param name="n"> ParsedPatternNode from within Visiting identifier node.</param>
        private void ProcessType(IdentifierNode node, Dictionary<string, Table> d, ParsedPatternNode n)
        {
            //Try find the table of the variable, it has to be always valid table name.
            if (!d.TryGetValue(node.value, out Table table))
                throw new ArgumentException("MatchVisitor, could not parse Table name.");
            else n.table = table;
        }


        /// <summary>
        /// Serves as a dividor of multiple patterns.
        /// Create new pattern and start its parsing.
        /// </summary>
        /// <param name="node"> Match Divider node </param>
        public void Visit(MatchDivider node)
        {
            currentPattern = new ParsedPattern();
            result.Add(currentPattern);
            node.next.Accept(this);
        }

        //Should never occur.
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }
    }



    /// <summary>
    ///Parent to every node.
    ///Gives Visit method.
    /// </summary>
    abstract class Node
    {
        public abstract void Accept<T>(IVisitor<T> visitor);
    }
    
    /// <summary>
    /// Certain nodes can form a chain. E.g variable node or vertex/edge node.
    /// Gives property next.
    /// </summary>
    abstract class QueryNode : Node
    {
        public Node next;
        public void AddNext(Node next)
        {
            this.next = next;
        }
    }
   
    /// <summary>
    /// Only vertices and edges inherit from this class.
    /// Gives varible node property to the edges and vertices.
    /// </summary>
    abstract class CommomMatchNode : QueryNode
    {
        public Node variable;

        public void AddVariable(Node v)
        {
            this.variable = v;
        }
    }

    /// <summary>
    /// Match and Select Nodes are only roots of subtrees when parsing. From them the parsing
    /// of query word starts.
    /// </summary>
    class MatchNode : QueryNode
    {
        public MatchNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class SelectNode : QueryNode
    {
        public SelectNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }


    /// <summary>
    /// Edge node and Vertex node represents vertex and edge in the parsing tree. 
    /// They hold next property that leads to a next vertex/edge or match divider.
    /// Match divider servers as a separator of multiple patterns in query.
    /// </summary>
    enum EdgeType { NotEdge, InEdge, OutEdge, AnyEdge };
    class EdgeNode : CommomMatchNode
    {
        EdgeType type;
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
        public void SetEdgeType(EdgeType type)
        {
            this.type = type;
        }
        public EdgeType GetEdgeType()
        {
            return this.type;
        }

    }
    class VertexNode : CommomMatchNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class MatchDivider : QueryNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Varible node serves as a holder for Name of varibles and possibly selection of their properties.
    /// Identifier node hold the real value of variable.
    /// </summary>
    class VariableNode : QueryNode
    {
        public Node name;
        public Node propName;

        public void AddName(Node n)
        {
            this.name = n;
        }

        public void AddProperty(Node p)
        {
            this.propName = p;
        }

        public bool IsEmpty()
        {
            if ((name == null) && (propName == null))
            {
                return true;
            }
            else return false;
        }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class IdentifierNode : Node
    {
        public string value { get; private set; }

        public IdentifierNode(string v) { this.value = v; }

        public void AddValue(string v) { this.value = v; }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Class used to shallow parsing match expression.
    /// Pattern contains single nodes with their corresponding attributes collected when parsed.
    /// Connections represents dictionary of other Parsed Patterns, where index is the index of pattern and string
    /// is variable that the two patterns are connected by.
    /// </summary>
    class ParsedPattern
    {
        public List<ParsedPatternNode> Pattern;
        public Dictionary<int, string> Connections;

        public ParsedPattern()
        {
            this.Pattern = new List<ParsedPatternNode>();
            this.Connections = new Dictionary<int, string>();
        }

        public void AddParsedPatternNode(ParsedPatternNode node)
        {
            this.Pattern.Add(node);
        }

        public int GetCount() => this.Pattern.Count;

        public ParsedPatternNode GetLastPasrsedPatternNode() => this.Pattern[this.Pattern.Count - 1];

    }


    /// <summary>
    /// Represents single Node when parsing match expression.
    /// </summary>
    class ParsedPatternNode
    {
        public bool isAnonymous;
        public bool isVertex;
        public Table table;
        public EdgeType edgeType;
        public string name;

        public ParsedPatternNode()
        {
            this.table = null;
            this.name = null;
            this.isVertex = true;
            this.isAnonymous = true;
        }

        public bool IsAnonymous() => this.isAnonymous;
        public bool IsVertex() => this.isVertex;
        public Table GetTable() => this.table;
        public EdgeType GetEdgeType() => this.edgeType;
        public string GetName() => this.name;


    }
}

/// old way of parsing 
    /*
    //Creates pattern for match from query.
    //We create new pattern for each division of a comma {viewed as MatchDivider node} 
    class MatchVisitor : IVisitor<List<List<BaseMatch>>>
    {
        List<List<BaseMatch>> result;
        List<BaseMatch> currentPattern;
        Dictionary<string, int> scope;
        Dictionary<string, Table> vTables;
        Dictionary<string, Table> eTables;
        bool readingName;
        bool readingVertex;
        int patternCount;


        public MatchVisitor(Scope s,
            Dictionary<string, Table> v, Dictionary<string, Table> e)
        {
            this.currentPattern = new List<BaseMatch>();
            this.result = new List<List<BaseMatch>>();
            this.scope = s.GetScopeVariables();
            this.vTables = v;
            this.eTables = e;
            this.readingName = true;
            this.readingVertex = true;
            this.patternCount = 0;
        }


        public List<List<BaseMatch>> GetResult()
        { return this.result; }

        /// <summary>
        /// Jumps to vertex node.
        /// All patterns must have at least one match.
        /// </summary>
        /// <param name="node"> Match node </param>
        public void Visit(MatchNode node)
        {
            //Create new pattern and start its parsing.
            result.Add(currentPattern);
            node.next.Accept(this);

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].Count <= 0)
                    throw new ArgumentException("MatchVisitor, failed to parse match expr.");
            }
        }




        public void Visit(VertexNode node)
        {
            this.readingVertex = true;
            VertexMatch vm = new VertexMatch();
            currentPattern.Add(vm);
            if (node.variable != null) node.variable.Accept(this);
            if (node.next != null) node.next.Accept(this);
        }



        public void Visit(EdgeNode node)
        {
            this.readingVertex = false;
            EdgeMatch em = new EdgeMatch();
            em.SetEdgeType(node.GetEdgeType());
            currentPattern.Add(em);
            if (node.variable != null) node.variable.Accept(this);
            if (node.next == null)
                throw new ArgumentException("MatchVisitor, missing end vertex from edge.");
            else node.next.Accept(this);

        }

        public void Visit(VariableNode node)
        {
            readingName = true;
            //It is not anonnymous field.
            if (node.name != null)
            {
                node.name.Accept(this);
            }
            //It has set type.
            if (node.propName != null)
            {
                readingName = false;
                node.propName.Accept(this);
            }

        }

        public void Visit(IdentifierNode node)
        {
            int relativePosition = currentPattern.Count - 1;
            if (readingName)
            {
                //It has name, it can not be anonnymous.
                currentPattern[relativePosition].SetAnnonymous(false);
                //Try if the variable is already in the scope.
                if (scope.TryGetValue(node.value, out int positionOfRepeated))
                {
                    //If it is, set the repeated status.
                    currentPattern[relativePosition].SetRepeated(true);
                    currentPattern[relativePosition].SetPositionOfRepeatedField(positionOfRepeated);
                }
                //It is new variable, then add it to scope.
                else scope.Add(node.value, GetAbsolutePosition());
            }
            else
            {
                if (readingVertex) ProcessType(relativePosition, node, vTables);
                else ProcessType(relativePosition, node, eTables);
            }
        }

        private void ProcessType(int p, IdentifierNode node, Dictionary<string, Table> d)
        {
            //Try find the table of the variable, it has to be always valid table name.
            if (!d.TryGetValue(node.value, out Table table))
                throw new ArgumentException("MatchVisitor, could not parse Table name.");
            else currentPattern[p].SetType(table);
        }

        private int GetAbsolutePosition()
        {
            int c = 0;
            for (int i = 0; i < result.Count; i++)
                c += result[i].Count;
            return (c - 1);
        }



        public void Visit(MatchDivider node)
        {
            //Create new pattern and start its parsing.
            currentPattern = new List<BaseMatch>();
            result.Add(currentPattern);
            patternCount++;
            node.next.Accept(this);
        }

        //Should never occur.
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }
    }
    */