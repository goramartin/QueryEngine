using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{
    struct Token
    {
        public enum TokenType 
        { 
                Match, Select,

                Asterix, Dot, DoubleDot, Comma, Dash, Less, Greater,  LeftParen, RightParen, LeftBrace, RightBrace,
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

    static class Tokenizer
    {
        static Dictionary<string, Token.TokenType> tokenTypes;
        static char EndOfQueryCharacter = ';';
        static Tokenizer()
        {
            tokenTypes = new Dictionary<string, Token.TokenType>();
            InitialiseRegistry();
        }
        
        public static List<Token> Tokenize(TextReader reader)
        {
            List<Token> tokens = new List<Token>();
            int ch = 0;
            while (true)
            {
                ch = reader.Read();
                if (ch == EndOfQueryCharacter) break;

                //Is one symbol token?
                if (tokenTypes.TryGetValue(((char)ch).ToString(), out Token.TokenType token))
                {
                    tokens.Add(new Token(null, token));
                }
                else if (char.IsWhiteSpace((char)ch))
                {
                    continue;
                }
                else if (Char.IsLetter((char)ch))
                {
                    //Get identifier value.
                    string ident = GetIdentifier((char)ch, reader);
                    
                    //Try whether it is a Query word.
                    Token.TokenType tok = default;
                    if (tokenTypes.TryGetValue(ident, out tok))
                    {
                        tokens.Add(new Token(null, tok));
                    }
                    //Else it is identifier.
                    else { tokens.Add(new Token(ident, Token.TokenType.Identifier)); }
                }
                else throw new ArgumentException($"{(char)ch} Found character that could not be parsed. Tokenizer.");
            }

            return tokens;
        }
       
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

        private static void RegisterToken(string str, Token.TokenType type)
        {

            if (tokenTypes.ContainsKey(str))
                throw new ArgumentException("TokenRegistry: Token Type already registered.");

            tokenTypes.Add(str, type);
        }

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

    static class Parser
    {
        static int position;
        static Parser() { position = 0; }

        static public int GetPosition() { return position; }

        static public void ResetPosition() { position = 0; }
        static private void IncrementPosition() { position++; }

        static public SelectNode ParseSelectExpr(List<Token> tokens) 
        {
            SelectNode selectNode = new SelectNode();

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
        static public MatchNode ParseMatchExpr(List<Token> tokens) 
        {
            MatchNode matchNode = new MatchNode();
            //IncrementPosition(); //Becuase we always expect we read Select and then we must increase position
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
        static private Node ParseVarForMatchExpr(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            //Expecting identifier. Can be empty.
            Node name = ParseIdentifierExrp(tokens);
            if (name != null) { IncrementPosition(); }
            variableNode.AddName(name);

            //
            if (CheckToken(position,Token.TokenType.DoubleDot, tokens))
            {
                IncrementPosition();
                Node identifierNode = ParseIdentifierExrp(tokens);
                if (identifierNode == null) throw new ArgumentException("VariableForMatchParser, exprected Indentifier after dot.");
                else variableNode.AddProperty(identifierNode);
                IncrementPosition();
            }
            if (variableNode.IsEmpty()) return null;
            else return variableNode;
        }
        static private Node ParseIdentifierExrp(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Identifier, tokens))
                return new IdentifierNode(tokens[position].strValue);
            else return null;
        }



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
            if (CheckToken(position, Token.TokenType.RightParen,tokens)) IncrementPosition();
            else return null;

            //Position incremented from leaving function PsrseVariable.
            //Parse Edge.
            Node edgeNode = ParseEdgeExpr(tokens);
            if (edgeNode != null) vertexNode.AddNext(edgeNode);

            //Always must return valid vertex.
            return vertexNode;
        }
        static private Node ParseEdgeExpr(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();

            Node anonymousEdge = ParseAnonymousEdge(tokens);
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
            else throw new ArgumentException("PArseEdge, expected vertex.");

            return edgeNode;
        }

        static private Node ParseEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();
            //Define type of edge.  in <...-, out -...>, any -...-
            EdgeType type = DefineEdgeType(tokens);
            if (type == EdgeType.NotEdge) return null;
            else edgeNode.SetType(type);
            IncrementPosition();

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

            //Skip end character of edge.  >,-
            IncrementPosition();

            return edgeNode;
        }
        static private Node ParseAnonymousEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();
            bool found = false;

            if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                    (CheckToken(position + 1, Token.TokenType.Greater, tokens)))
            {
                edgeNode.SetType(EdgeType.OutEdge);
                found = true;
                position += 2;
            }
            else if (CheckToken(position, Token.TokenType.Dash, tokens) &&
                    (!CheckToken(position +1, Token.TokenType.LeftBrace, tokens)))
            {
                edgeNode.SetType(EdgeType.AnyEdge);
                found = true;
                position += 1;
            }
            else if (CheckToken(position, Token.TokenType.Less, tokens) &&
                    (CheckToken(position + 1, Token.TokenType.Dash, tokens)))
            {
                edgeNode.SetType(EdgeType.InEdge);
                found = true;
                position += 2;
            }

            if (found) return edgeNode;
            else return null;

        }
        static private EdgeType DefineEdgeType(List<Token> tokens)
        {
            //Any edge -[..]-
            if (PredictEgeType(Token.TokenType.Dash, Token.TokenType.Dash, tokens))
                return EdgeType.AnyEdge;
            //In edge <[..]-
            else if (PredictEgeType(Token.TokenType.Less, Token.TokenType.Dash, tokens))
                return EdgeType.InEdge;
            //Out edge -[..]>
            else if (PredictEgeType(Token.TokenType.Dash, Token.TokenType.Greater, tokens))
                return EdgeType.OutEdge;
            else return EdgeType.NotEdge;
        }
        static private bool PredictEgeType(Token.TokenType t1, Token.TokenType t2, List<Token> tokens)
        {
            // -[e]>
            int predictionOne = position + 4;
            // -[:prop]>
            int predictionTwo = position + 5;
            // -[e:x]>
            int predictionThree = position + 6;
            if (CheckToken(position, t1, tokens) && 
                (CheckToken(predictionOne, t2, tokens) || 
                 CheckToken(predictionTwo, t2, tokens) || 
                 CheckToken(predictionThree, t2, tokens))) return true;
            else return false;

        }

        //Check for token on position given.
        static private bool CheckToken(int p, Token.TokenType type, List<Token> tokens)
        {
            if (p < tokens.Count && tokens[p].type == type)
            {
                return true;
            }
            return false;

        }
    }

    interface IVisitor<T>
    {
        T Visit(IdentifierNode node);
        T Visit(VariableNode node);
        T Visit(VertexNode node);
        T Visit(EdgeNode node);
        T Visit(MatchNode node);
        T Visit(SelectNode node);
    }

    class SelectVisitor : IVisitor<bool>
    {


        public bool Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(VariableNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }

        

        public bool Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(EdgeNode node)
        {
            throw new NotImplementedException();
        }
    }

    class MatchVisitor : IVisitor<bool>
    {

        public bool Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(VariableNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(EdgeNode node)
        {
            throw new NotImplementedException();
        }
    }



    abstract class Node
    {
        public abstract T Accept<T>(IVisitor<T> visitor);
    }
    abstract class QueryNode :Node
    {
        public  Node next;
        public void AddNext(Node next)
        {
            this.next = next;
        }
    }
    abstract class CommomMatchNode : QueryNode
    {
        Node variable;

        public void AddVariable(Node v)
        {
            this.variable = v;
        }
    }


    class MatchNode : QueryNode
    {
        public MatchNode()
        {
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    class SelectNode : QueryNode
    {
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }


    enum EdgeType{ InEdge,OutEdge,AnyEdge, NotEdge};
    class EdgeNode : CommomMatchNode
    {
        EdgeType type;
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
        public void SetType(EdgeType type)
        {
            this.type = type;
        }
    }

    class VertexNode : CommomMatchNode
    {
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }


    class VariableNode : QueryNode
    {
        Node name;
        Node propName;

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

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    class IdentifierNode : Node
    {
        string value;

        public IdentifierNode(string v) { this.value = v; }

        public void AddValue(string v) { this.value = v; }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
