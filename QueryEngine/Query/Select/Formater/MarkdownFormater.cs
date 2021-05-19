using System.Collections.Generic;
using System.IO;

namespace QueryEngine 
{ 

    /// <summary>
    /// A format is printed as a markdown table.
    /// </summary>
    internal sealed class MarkDownFormater : Formater
    {
        public MarkDownFormater(int columnCount, TextWriter writer) : base(columnCount, writer) { }


        /// <summary>
        /// Adds a word to a format and separates it with a | character to end a column in markdown syntax.. 
        /// </summary>
        /// <param name="word"> A word to format. </param>
        public override void AddToFormat(string word)
        {
            if (this.ColumnsFilled == 0) this.stringBuilder.Append('|');

            this.stringBuilder.Append(word);
            this.stringBuilder.Append('|');

            this.ColumnsFilled++;
            if (this.ColumnsFilled == this.ColumnCount) this.Flush();
        }

        /// <summary>
        /// Formates a given header.
        /// Each column is printed and below is printed a dash delimeter to separate
        /// the header and the results. Columns are also | separated on sides.
        /// </summary>
        /// <param name="variables"> A header format. </param>
        public override void FormatHeader(List<ExpressionToStringWrapper> variables)
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
        /// Flushed the string builder and prepares printing of the next row.
        /// </summary>
        public override void Flush()
        {
            this.writer.WriteLine(this.stringBuilder.ToString());
            this.stringBuilder.Clear();
            this.ColumnsFilled = 0;
        }
    }
}
