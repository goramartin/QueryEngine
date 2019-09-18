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
                    string ident = GetIdentifier((char)ch, reader);
                    Token.TokenType t = default;
                    if (tokenTypes.TryGetValue(ident, out t))
                    {
                        tokens.Add(new Token(null, token));
                    }
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
            RegisterToken("SELECT", Token.TokenType.Match);
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
        public MatchNode(Node n)
        {
            this.next = n;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    class SelectNode : QueryNode
    {
        public SelectNode(Node n)
        {
            this.next = n;
        }
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
