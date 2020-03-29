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
        public static Dictionary<string, string> fileEndings;

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


        static Formater()
        {
            fileEndings = new Dictionary<string, string>();
            fileEndings.Add("simple", ".txt");
            fileEndings.Add("markdown", ".md");
        }

        protected Formater()
        {
            this.stringBuilder = new StringBuilder();
            this.columnsFilled = 0;
        }

        protected Formater(int columnCount, TextWriter writer): this()
        {
            this.columnCount = columnCount;
            this.writer = writer;
        }

        public abstract void FormatHeader(List<PrinterVariable> header);
        public abstract void AddToFormat(string word);
        public abstract void Flush();


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

        public override void AddToFormat(string word)
        {
            this.stringBuilder.Append(word);
            this.PadWithChar(Formater.BaseColumnLength - word.Length, ' ');
            this.columnsFilled++;
            if (this.columnsFilled == this.columnCount) this.Flush();
        }


        public override void FormatHeader(List<PrinterVariable> variables)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                string tmp = variables[i].GetHeader();
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

        public override void Flush()
        {
            this.writer.WriteLine(this.stringBuilder.ToString());
            this.stringBuilder.Clear();
            this.columnsFilled = 0;
        }

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

        public override void AddToFormat(string word)
        {
            if (this.columnsFilled == 0) this.stringBuilder.Append('|');
            
            this.stringBuilder.Append(word);
            this.stringBuilder.Append('|');

            this.columnsFilled++;
            if (this.columnsFilled == this.columnCount) this.Flush();
        }


        public override void FormatHeader(List<PrinterVariable> variables)
        {
            this.stringBuilder.Append('|');
            for (int i = 0; i < variables.Count; i++)
            {
                this.stringBuilder.Append(variables[i].GetHeader());
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


        public override void Flush()
        {
            this.writer.WriteLine(this.stringBuilder.ToString());
            this.stringBuilder.Clear();
            this.columnsFilled = 0;
        }
    }

}
