/*! \file 
 
    This file contains definition of a tokenizer.
    Tokenizer tokenizes the inputed query.
    It creates a list of tokens that are subsequently parsed.
 
    Tokenizer has set ending character of a query.
*/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QueryEngine
{
    

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
            RegisterToken("AS", Token.TokenType.AsLabel);
            RegisterToken("as", Token.TokenType.AsLabel);
            RegisterToken("BY", Token.TokenType.By);
            RegisterToken("by", Token.TokenType.By);
            RegisterToken("ORDER", Token.TokenType.Order);
            RegisterToken("order", Token.TokenType.Order);
            RegisterToken("asc", Token.TokenType.Asc);
            RegisterToken("ASC", Token.TokenType.Asc);
            RegisterToken("desc", Token.TokenType.Desc);
            RegisterToken("DESC", Token.TokenType.Desc);
        }

    }
}
