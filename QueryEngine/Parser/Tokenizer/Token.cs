/*! \file 
This file contains definition of a token.
Token is used during tokenisation of an input query.
*/

namespace QueryEngine
{
    /// <summary>
    /// Represents single token when parsing. Token type is a type of the token.
    /// </summary>
    internal struct Token
    {
        public enum TokenType
        {
            Match,
            Select,
            Order,
            Group,
            By,
            Asc,
            Desc,

            Asterix,
            Dot,
            DoubleDot,
            Comma,
            Dash,
            Less,
            Greater,
            LeftParen,
            RightParen,
            LeftBrace,
            RightBrace,
            Identifier,
            AsLabel,
        }
        public readonly string strValue;
        public TokenType type;

        public Token(string value, TokenType type)
        {
            this.strValue = value;
            this.type = type;
        }

        public override string ToString()
        {
            return type.ToString() + ( this.strValue == null ? "" : ( " : " + this.strValue));
        }
    }
}
