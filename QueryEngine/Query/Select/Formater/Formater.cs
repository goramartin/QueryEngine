using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QueryEngine
{
    /// <summary>
    /// A type of formater defines the format of the output.
    /// </summary>
    public enum FormaterType { 
        
        /// <summary>
        /// Prints results in a form of a simple table where results are divided by spaces.
        /// </summary>
        Simple,
        
        /// <summary>
        /// Prints results in a form of a mark down table.
        /// </summary>
        MarkDown }


    /// <summary>
    /// A class defining how resulting table, when printing results, will look like.
    /// </summary>
    internal abstract class Formater
    {
        /// <summary>
        /// Endings of files based on a format. 
        /// </summary>
        public static Dictionary<FormaterType, string> FileEndings { get; }

        protected StringBuilder stringBuilder;
        protected int ColumnCount { get; }

        /// <summary>
        /// Some formats define normalised lenght of a column.
        /// </summary>
        static protected int BaseColumnLength => 30;
        
        /// <summary>
        /// A state which remembers which column in a row is being filled next.
        /// </summary>
        protected int ColumnsFilled { get; set; }
        
        protected TextWriter writer;

        /// <summary>
        /// Inicialises dictionaries with valid types and valid file endings.
        /// </summary>
        static Formater()
        {
            FileEndings = new Dictionary<FormaterType, string>();
            FileEndings.Add(FormaterType.Simple, ".txt");
            FileEndings.Add(FormaterType.MarkDown, ".md");
        }

        protected Formater()
        {
            this.stringBuilder = new StringBuilder();
            this.ColumnsFilled = 0;
        }

        /// <summary>
        /// A base class construtor.
        /// </summary>
        /// <param name="columnCount"> A column number of a printed table.</param>
        /// <param name="writer"> Where to write output. </param>
        protected Formater(int columnCount, TextWriter writer): this()
        {
            this.ColumnCount = columnCount;
            this.writer = writer;
        }

        /// <summary>
        /// Formats the header of a table.
        /// </summary>
        /// <param name="header"> A header format. </param>
        public abstract void FormatHeader(List<ExpressionToStringWrapper> header);
        
        /// <summary>
        /// Adds a word to a row and formats it.
        /// </summary>
        /// <param name="word"> A word to format. </param>
        public abstract void AddToFormat(string word);
        
        /// <summary>
        /// Flushed the string builder to a writer.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// A factory method.
        /// </summary>
        /// <param name="formater"> A formater type. </param>
        /// <param name="columnCount"> A number of columns in a printed table. </param>
        /// <param name="writer"> An output writer. </param>
        /// <returns> A formater instance. </returns>
        public static Formater Factory(FormaterType formater, int columnCount, TextWriter writer)
        {
            if (writer == null) 
                throw new ArgumentNullException($"Formater factory, was given writer as null.");
            if (columnCount <= 0)
                throw new ArgumentException($"Formater factory, was given invalid number of columns. Columns = {columnCount}.");
            else { }

            if (formater == FormaterType.Simple)
                return new SimpleFormater(columnCount, writer);
            else if (formater == FormaterType.MarkDown)
                return new MarkDownFormater(columnCount, writer);
            else throw new ArgumentException($"Formater factory, formater type does not exist. Formater = {formater}");
        }
    }
}
