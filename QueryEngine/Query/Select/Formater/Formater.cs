/*! \file
This file includes definition of Formater which is used by a printer.
Formater formats out put of a select expression.
So far there are two formaters and those are simple and markdown formater.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QueryEngine
{

    public enum FormaterType { Simple, MarkDown }
    /// <summary>
    /// Class defining how resulting table when printing results will look like.
    /// </summary>
    internal abstract class Formater
    {
        /// <summary>
        /// Endings of files based on format. 
        /// </summary>
        public static Dictionary<FormaterType, string> FileEndings { get; }

        protected StringBuilder stringBuilder;
        protected int ColumnCount { get; }

        /// <summary>
        /// Some formats define normalise lenght of one part of a row.
        /// </summary>
        static protected int BaseColumnLength => 30;
        
        /// <summary>
        /// State which remembers which column in a row is being filled next.
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
        /// Base class construtor.
        /// </summary>
        /// <param name="columnCount">Column number of a printed table.</param>
        /// <param name="writer"> Where to write output. </param>
        protected Formater(int columnCount, TextWriter writer): this()
        {
            this.ColumnCount = columnCount;
            this.writer = writer;
        }

        /// <summary>
        /// Formats header of a table.
        /// </summary>
        /// <param name="header">Header format. </param>
        public abstract void FormatHeader(List<ExpressionToStringWrapper> header);
        
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
