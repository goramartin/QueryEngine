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

                Asterix, Dot, DoubleDot, Comma, Dash, Less, Greater, 
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
                    //Get identifier value
                    string ident = GetIdentifier((char)ch, reader);
                    
                    //Try whether it is a Query word
                    Token.TokenType tok = default;
                    if (tokenTypes.TryGetValue(ident, out tok))
                    {
                        tokens.Add(new Token(null, tok));
                    }
                    //Else it is identifier
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
            RegisterToken("MATCH", Token.TokenType.Match);
            RegisterToken("SELECT", Token.TokenType.Select);
        }

    }

    class Parser
    {
        int position = 0;
        public Parser() { }

        public void ResetPosition() { this.position = 0; }
        private void IncrementPosition() { this.position++; }

        public Node ParseSelectExpr(List<Token> tokens) 
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

        public Node ParseMatchExpr(List<Token> tokens) 
        {
            return null;
        }

        private Node ParseVariableExpr(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            //Expecting identifier
            Node name = ParseIdentifierExrp(tokens);
            if (name == null) return null;
            else variableNode.AddName(name);
            IncrementPosition();


            //case of property name .PropName , if there is dot, there must follow identifier
            if (CheckToken(position, Token.TokenType.Dot, tokens))
            {
                IncrementPosition();
                Node identifierNode = ParseIdentifierExrp(tokens);
                if (identifierNode == null) throw new ArgumentException("VariableParser, exprected Indentifier after dot.");
                else variableNode.AddProperty(identifierNode);
                IncrementPosition();
            }

            //comma signals there is another variable, next variablenode must follow
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                IncrementPosition();
                Node nextVariableNode = ParseVariableExpr(tokens);
                if (nextVariableNode == null) throw new ArgumentException("VariableParser, exprected Indentifier after comma.");
                else variableNode.AddNext(nextVariableNode);
            }
            return variableNode;
        }
    


        private Node ParseIdentifierExrp(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Identifier, tokens))
                return new IdentifierNode(tokens[position].strValue);
            else return null;
        }

        private Node ParseVertexExpr(List<Token> tokens)
        {
            return null;
        }

        private Node ParseEdgeExpr(List<Token> tokens)
        {
            return null;
        }

        private bool CheckToken(int position, Token.TokenType type, List<Token> tokens)
        {
            if (position < tokens.Count && tokens[position].type == type)
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
        T Visit(IncomingEdgeNode node);
        T Visit(OutgoingEdgeNode node);
        T Visit(AnyEdgeNode node);
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

        public bool Visit(IncomingEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(OutgoingEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(AnyEdgeNode node)
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

        public bool Visit(IncomingEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(OutgoingEdgeNode node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(AnyEdgeNode node)
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


    class IncomingEdgeNode : CommomMatchNode
    {
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    class AnyEdgeNode : CommomMatchNode
    {
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    class OutgoingEdgeNode : CommomMatchNode
    {
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
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
