﻿/*! \file 
This file contains definition of a tokenizer.
The tokenizer tokenizes the inputed query.
It creates a List of tokens that are subsequently parsed.
The tokenizer has set ending character of a query.

The tokenizer reads input by single characters and stops when it reaches a character that signals the end of 
user input. When it reads the character it firstly tries to find a token from the registry (a single
character token) otherwise it expects a string that is either a token or an identifier (which is also a token).

Notice that only inputted identifiers are considered case sensitive (e.g. names of variables).
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace QueryEngine
{
   
    /// <summary>
    /// A class takes console input and creates tokens based on their string representation.
    /// </summary>
    internal static class Tokenizer
    {
        /// <summary>
        /// A Dictionary of all possible tokens.
        /// </summary>
        private static Dictionary<string, Token.TokenType> tokenTypes;
        /// <summary>
        /// A character that signals and of the query.
        /// </summary>
        private static char EndOfQueryCharacter => ';';
        static Tokenizer()
        {
            tokenTypes = new Dictionary<string, Token.TokenType>();
            InitialiseRegistry();
        }

        /// <summary>
        /// Generates a stream from a string.
        /// Be careful with the encoding.
        /// </summary>
        /// <param name="input"> An input string. </param>
        /// <returns> A stream containing inputed string. </returns>
        private static Stream GenerateStreamFromString(string input)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }


        /// <summary>
        /// Tokenizes the input query from string.
        /// Creates a stream from the string and calls a method Tokenize(TextReader)
        /// </summary>
        /// <param name="input"> A query as a string. </param>
        /// <returns> A List of parsed tokens. </returns>
        public static List<Token> Tokenize(string input)
        {
            List<Token> tokens;
            using (var stream = GenerateStreamFromString(input))
            {
                tokens = Tokenize(new StreamReader(stream));
            }

            return tokens;
        }

        /// <summary>
        /// Reads input char by char and parses keywords and creates tokens based on the keywords.
        /// </summary>
        /// <param name="reader"> A reader that reads input from a string or console. </param>
        /// <returns> A List of parsed tokens </returns>
        public static List<Token> Tokenize(TextReader reader)
        {
            // A result storage.
            List<Token> tokens = new List<Token>();
            int ch = 0;


            while (true)
            {
                ch = reader.Read();
                if (ch == EndOfQueryCharacter) break;

                // Is one symbol a token?
                // This token does not have a string value only semantic value.
                if (tokenTypes.TryGetValue(((char)ch).ToString(), out Token.TokenType token))
                {
                    tokens.Add(new Token(null, token));
                }
                // Skip reading whitespace characters.
                else if (char.IsWhiteSpace((char)ch))
                {
                    continue;
                }
                // If the character is a normal letter, we parse the whole consecutive word.
                else if (Char.IsLetter((char)ch))
                {
                    // Get an identifier value.
                    string ident = GetIdentifier((char)ch, reader);

                    // Try whether it is a query word.
                    // Query words are always considered in lower case.
                    // Query word is a SELECT, MATCH ...
                    if (tokenTypes.TryGetValue(ident.ToLower(), out Token.TokenType tok))
                    {
                        tokens.Add(new Token(null, tok));
                    }
                    // Else it is an identifier that has got a string value.
                    // Identifiers are case sensitive.
                    else { tokens.Add(new Token(ident, Token.TokenType.Identifier)); }
                }
                else throw new ArgumentException($"{(char)ch} Found character that could not be parsed. Tokenizer.");
            }

            return tokens;
        }


        /// <summary>
        /// Reads a single word from an input.
        /// </summary>
        /// <param name="ch"> The first already consumed character. </param>
        /// <param name="reader"> A console reader. </param>
        /// <returns> A word from input starting with the character from parameters. </returns>
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
        /// Inserts a token with its input representaion into the token registry.
        /// </summary>
        /// <param name="str"> A string representation in input.</param>
        /// <param name="type"> A token type. </param>
        private static void RegisterToken(string str, Token.TokenType type)
        {

            if (tokenTypes.ContainsKey(str))
                throw new ArgumentException("TokenRegistry: Token Type already registered.");

            tokenTypes.Add(str, type);
        }

        /// <summary>
        /// Inserts all possible tokens with their assiciative string values in input.
        /// Note that Identifier token is not included.
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
            RegisterToken("match", Token.TokenType.Match);
            RegisterToken("select", Token.TokenType.Select);
            RegisterToken("as", Token.TokenType.AsLabel);
            RegisterToken("by", Token.TokenType.By);
            RegisterToken("order", Token.TokenType.Order);
            RegisterToken("asc", Token.TokenType.Asc);
            RegisterToken("desc", Token.TokenType.Desc);
            RegisterToken("group", Token.TokenType.Group);
        }
    }
}
