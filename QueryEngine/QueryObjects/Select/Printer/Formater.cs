
/*! \file
  
    This file includes definition of Formater which is used by a printer.
    Formater formats out put of a select expression.
    So far there are two formaters and those are simple and markdown formater.
 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class defining how resulting table when printing results will look like.
    /// </summary>
    abstract class Formater
    {
        /// <summary>
        /// Endings of files based on format. 
        /// </summary>
        public static Dictionary<string, string> fileEndings { get; }

        /// <summary>
        /// Contains valid Formaters.
        /// </summary>
        public static HashSet<string> Formaters { get;  }

        protected StringBuilder stringBuilder;
        protected int columnCount { get; }

        /// <summary>
        /// Some formats define normalise lenght of one part of a row.
        /// </summary>
        static protected int BaseColumnLength => 30;
        
        /// <summary>
        /// State which remembers which column in a row is being filled next.
        /// </summary>
        protected int columnsFilled { get; set; }
        
        protected TextWriter writer;

        /// <summary>
        /// Inicialises dictionaries with valid types and valid file endings.
        /// </summary>
        static Formater()
        {
            fileEndings = new Dictionary<string, string>();
            fileEndings.Add("simple", ".txt");
            fileEndings.Add("markdown", ".md");

            Formaters = new HashSet<string>();
            Formaters.Add("simple");
            Formaters.Add("markdown");
        }

        protected Formater()
        {
            this.stringBuilder = new StringBuilder();
            this.columnsFilled = 0;
        }

        /// <summary>
        /// Base class construtor.
        /// </summary>
        /// <param name="columnCount">Column number of a printed table.</param>
        /// <param name="writer"> Where to write output. </param>
        protected Formater(int columnCount, TextWriter writer): this()
        {
            this.columnCount = columnCount;
            this.writer = writer;
        }

        /// <summary>
        /// Formats header of a table.
        /// </summary>
        /// <param name="header">Header format. </param>
        public abstract void FormatHeader(List<PrintVariable> header);
        
        /// <summary>
        /// Adds a word to a row and formats it.
        /// </summary>
        /// <param name="word"> Word to format. </param>
        public abstract void AddToFormat(string word);
        
        /// <summary>
        /// Flushed string builder to a writer.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Formater factory.
        /// </summary>
        /// <param name="formater"> Formater type. </param>
        /// <param name="columnCount"> Number of columns in a printed table. </param>
        /// <param name="writer"> Output writer. </param>
        /// <returns> Formater instance. </returns>
        public static Formater FormaterFactory(string formater, int columnCount, TextWriter writer)
        {
            if (writer == null) 
                throw new ArgumentNullException($"Formater factory, was given writer as null.");
            if (columnCount <= 0)
                throw new ArgumentException($"Formater factory, was given invalid number of columns. Columns = {columnCount}.");
            else { }

            if (formater == "simple")
                return new SimpleFormater(columnCount, writer);
            else if (formater == "markdown")
                return new MarkDownFormater(columnCount, writer);
            else throw new ArgumentException($"Formater factory, formater type does not exist. Formater = {formater}");
        }
    }


    /// <summary>
    /// Simple formater prints output with normalised length of a one column in a row.
    /// No table and separators are only spaces.
    /// </summary>
    class SimpleFormater: Formater
    {
        public SimpleFormater(int columnCount, TextWriter writer) : base(columnCount,writer) { }

        /// <summary>
        /// Adds word to a format and separates it with a space character. 
        /// </summary>
        /// <param name="word"> Word to add to a format. </param>
        public override void AddToFormat(string word)
        {
            this.stringBuilder.Append(word);
            this.PadWithChar(Formater.BaseColumnLength - word.Length, ' ');
            this.columnsFilled++;
            if (this.columnsFilled == this.columnCount) this.Flush();
        }


        /// <summary>
        /// Formates a given header.
        /// Each column is printed and below is printed a dash delimeter to separate
        /// header and results.
        /// </summary>
        /// <param name="variables"> Header format. </param>
        public override void FormatHeader(List<PrintVariable> variables)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                string tmp = variables[i].ToString();
                this.stringBuilder.Append(tmp);
                this.PadWithChar(Formater.BaseColumnLength - tmp.Length, ' ');
            }
           
            this.Flush();

            for (int i = 0; i < this.columnCount; i++)
            {
                this.PadWithChar(Formater.BaseColumnLength, '-');
            }
            this.Flush();
        }

        /// <summary>
        /// Flushed string builder and prepares for printing next row.
        /// </summary>
        public override void Flush()
        {
            this.writer.WriteLine(this.stringBuilder.ToString());
            this.stringBuilder.Clear();
            this.columnsFilled = 0;
        }

        /// <summary>
        /// Adds given number of character to a string builder.
        /// </summary>
        /// <param name="count"> Number of characters. </param>
        /// <param name="c"> Character to add. </param>
        private void PadWithChar(int count, char c)
        {
            if (count <= 0) return;
            else
            {
                for (int i = 0; i < count; i++)
                {
                    this.stringBuilder.Append(c);
                }
            }
        }

    }
    

    /// <summary>
    /// Format is printed as a markdown table.
    /// </summary>
    class MarkDownFormater : Formater
    {
        public MarkDownFormater(int columnCount, TextWriter writer) : base(columnCount, writer) { }


        /// <summary>
        /// Adds word to a format and separates it with a | character to end a column in markdown syntax.. 
        /// </summary>
        /// <param name="word"> Word to add to a format. </param>
        public override void AddToFormat(string word)
        {
            if (this.columnsFilled == 0) this.stringBuilder.Append('|');
            
            this.stringBuilder.Append(word);
            this.stringBuilder.Append('|');

            this.columnsFilled++;
            if (this.columnsFilled == this.columnCount) this.Flush();
        }

        /// <summary>
        /// Formates a given header.
        /// Each column is printed and below is printed a dash delimeter to separate
        /// header and results. Columns are also | separated on sides.
        /// </summary>
        /// <param name="variables"> Header format. </param>
        public override void FormatHeader(List<PrintVariable> variables)
        {
            this.stringBuilder.Append('|');
            for (int i = 0; i < variables.Count; i++)
            {
                this.stringBuilder.Append(variables[i].ToString());
                this.stringBuilder.Append('|');
            }
            this.Flush();

            this.stringBuilder.Append('|');
            for (int i = 0; i < variables.Count; i++)
            {
                this.stringBuilder.Append("---|");
            }
            this.Flush();
        }

        /// <summary>
        /// Flushed string builder and prepares for printing next row.
        /// </summary>
        public override void Flush()
        {
            this.writer.WriteLine(this.stringBuilder.ToString());
            this.stringBuilder.Clear();
            this.columnsFilled = 0;
        }
    }

}
