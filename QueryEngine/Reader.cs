using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QueryEngine
{
    //must return null when finished reading
    interface IReader: IDisposable
    {
        string Read();
    }
    interface IWordReader :IReader
    {
        string GetWord();
    } 

    /// <summary>
    /// Class for reading text files.
    /// Gets one file and pulls each word from the file.
    /// If it is at the end of file, it returns null.
    /// Delimeters can be set.
    /// </summary>
    class WordReader : IWordReader
    {
        StringBuilder wordBuilder;
        StreamReader fileReader;
        char[] delimeters = new char[] { '\r', '\n', '\t', ' ', '"', ',' }; //delimeters between words
        bool end;

        public WordReader(string fileName)
        {
            this.wordBuilder = new StringBuilder();
            this.end = false;
            try
            {
                if (!File.Exists(fileName)) throw new IOException();
                else this.fileReader = new StreamReader(fileName);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public string GetWord()
        {
            int character = 0;
            int characterPeeked = 0;

            if (this.end) return null;
            this.wordBuilder.Clear();

            //peek next character
            while ((characterPeeked = this.fileReader.Peek()) != -1)
            {
                //the character is delimeter
                if (this.delimeters.Contains((char)characterPeeked))
                {
                    //any chars yet, consume the whitespace
                    if (this.wordBuilder.Length == 0)
                    {
                        character = this.fileReader.Read();
                    }
                    //read a first whitespace after reading a word, return the word
                    else return this.wordBuilder.ToString();
                }
                //the character is normal letter
                else
                {
                    //add the letter to stringBuilder
                    character = this.fileReader.Read();
                    this.wordBuilder.Append((char)character);
                }

            }
            //not returned last word
            if (this.wordBuilder.Length > 0)
            {
                this.end = true;
                return this.wordBuilder.ToString();
            }
            //finished reading
            else
            {
                return null;
            }
        }

        public string Read()
        {
            return this.GetWord();
        }


        public void Dispose()
        {
            if (fileReader != null)
            {
                this.fileReader.Close();
            }
        }
    }

    class Reader : IReader
    {
        StringBuilder wordBuilder;
        StreamReader fileReader;
        bool end;

        public Reader(string fileName)
        {
            this.wordBuilder = new StringBuilder();
            this.end = false;
            try
            {
                if (!File.Exists(fileName)) throw new IOException();
                else this.fileReader = new StreamReader(fileName);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public void Dispose()
        {
            this.fileReader.Close();
        }

        public string Read()
        {
            int characterPeeked = 0;

            if (this.end) return null;

            while ((characterPeeked = fileReader.Peek()) != -1)
            {
                if (char.IsWhiteSpace((char)characterPeeked))
                    fileReader.Read();
                else if (char.IsLetter((char)characterPeeked))
                    return GetWord((char)characterPeeked);
                else
                {
                    fileReader.Read();
                    return ((char)characterPeeked).ToString();
                }
            }
            this.end = true;
            return null;
        }

        private string GetWord(char ch)
        {
            fileReader.Read();
            wordBuilder.Clear();
            wordBuilder.Append(ch);

            while (true)
            {
                int peekedChar = fileReader.Peek();
                if (char.IsLetter((char)peekedChar))
                {
                    wordBuilder.Append((char)peekedChar);
                    fileReader.Read();
                }
                else { break; }
            }

            return wordBuilder.ToString();

        }


    }

}
