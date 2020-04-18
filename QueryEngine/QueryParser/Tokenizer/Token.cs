/*! \file 
 
    This file contains definition of a token.
    Token is used during tokenisation of an input query.
 
     */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Identifier, AsLabel
        }
        public readonly string strValue;
        public TokenType type;

        public Token(string value, TokenType type)
        {
            this.strValue = value;
            this.type = type;
        }
    }
}
