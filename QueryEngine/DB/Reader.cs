/*! \file 
This file contains definition of file readers.
These readers are used for loading a graph.
There are two readers, one reader is used for parsing edges and vertices from a file.
The second one is used for parsing a JSON schema for table definitions.
The difference is that reader for the schema must not omit certain special characters.
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
    /// Interface for reading text files.
    /// </summary>
    internal interface IReader: IDisposable
    {
        string Read();
    }
    /// <summary>
    /// Interface to reader entire words from a file.
    /// </summary>
    internal interface IWordReader :IReader
    {
        string GetWord();
    } 

    /// <summary>
    /// Class for reading text files.
    /// Gets one file and pulls each word from the file.
    /// If it is at the end of file, it returns null.
    /// Delimeters can be set.
    /// Note this reader is used to read data files with edges and vertices.
    /// It needs to read words from a stream but must ommit certain special characters.
    /// </summary>
    internal class DataFileReader : IWordReader
    {
        StringBuilder wordBuilder;
        StreamReader fileReader;
        char[] delimeters = new char[] { '\r', '\n', '\t', ' ', '"', ',' };
        bool end;

        /// <summary>
        /// Creates reader and opens given file for reading.
        /// </summary>
        /// <param name="fileName"> File name to open. </param>
        public DataFileReader(string fileName)
        {
            this.wordBuilder = new StringBuilder();
            this.end = false;
            try
            {
                this.fileReader = new StreamReader(fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Stripes words from a stream.
        /// Omits whitespaces.
        /// </summary>
        /// <returns> Words from a stream. </returns>
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

        /// <summary>
        /// Calls get words method on this class.
        /// </summary>
        /// <returns>Returns a word.</returns>
        public string Read()
        {
            return this.GetWord();
        }

        /// <summary>
        /// Flushes reader.
        /// </summary>
        public void Dispose()
        {
            if (fileReader != null)
            {
                this.fileReader.Close();
            }
        }
    }

     /// <summary>
    /// A class for reading text files.
    /// Gets one file and pulls each non whitespace character from the file.
    /// If it is at the end of file, it returns null.
    /// Note this reader is used for reading a file with definition of tables in JSON format.
    /// Reader stripes words or a special characters from a file and returns it.
    /// </summary>
    internal class TableFileReader : IReader
    {

        /// <summary>
        /// Builder for word striping.
        /// </summary>
        StringBuilder wordBuilder;
        
        /// <summary>
        /// File reader.
        /// </summary>
        StreamReader fileReader;
        
        /// <summary>
        /// Specifies whether we reached end of file.
        /// </summary>
        bool end;

        /// <summary>
        /// Creates reader. Tries to open file for reader.
        /// Throw io exception. 
        /// </summary>
        /// <param name="fileName"> File to open for reading. </param>
        public TableFileReader(string fileName)
        {
            this.wordBuilder = new StringBuilder();
            this.end = false;
            try
            {
               this.fileReader = new StreamReader(fileName);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            if (this.fileReader != null)
            {
                this.fileReader.Close();
            }
        }

        /// <summary>
        /// Either reads a single word or a special non letter character.
        /// Skipps whitespaces.
        /// </summary>
        /// <returns></returns>
        public string Read()
        {
            int characterPeeked = 0;

            if (this.end) return null;

            // Peek a character
            while ((characterPeeked = fileReader.Peek()) != -1)
            {
                // Skip whitespace character.
                if (char.IsWhiteSpace((char)characterPeeked))
                    fileReader.Read();
                // If letter found strip entire word
                else if (char.IsLetter((char)characterPeeked))
                    return GetWord((char)characterPeeked);
                else
                {
                    // Retuning non letter characters such as ,./'"|
                    fileReader.Read();
                    return ((char)characterPeeked).ToString();
                }
            }
            this.end = true;
            return null;
        }

        /// <summary>
        /// Stripes entire word from a stream.
        /// </summary>
        /// <param name="ch"> First consumed character from the stream. </param>
        /// <returns> Word from a stream. </returns>
        private string GetWord(char ch)
        {
            fileReader.Read();
            wordBuilder.Clear();
            wordBuilder.Append(ch);

            // While you encounter normal letters keep appending them to a builder.
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
