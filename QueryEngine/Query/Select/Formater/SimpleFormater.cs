using System.Collections.Generic;
using System.IO;

namespace QueryEngine
{
    /// <summary>
    /// Simple formater prints output with normalised length of a one column in a row.
    /// </summary>
    internal sealed class SimpleFormater : Formater
    {
        public SimpleFormater(int columnCount, TextWriter writer) : base(columnCount, writer) { }

        /// <summary>
        /// Adds a word to a format and separates it with space characters. 
        /// </summary>
        /// <param name="word"> A word to format. </param>
        public override void AddToFormat(string word)
        {
            this.stringBuilder.Append(word);
            this.PadWithChar(Formater.BaseColumnLength - word.Length, ' ');
            this.ColumnsFilled++;
            if (this.ColumnsFilled == this.ColumnCount) this.Flush();
        }


        /// <summary>
        /// Formates a given header.
        /// Each column is printed and below is printed a dash delimeter to separate
        /// header and results.
        /// </summary>
        /// <param name="variables"> A header format. </param>
        public override void FormatHeader(List<ExpressionToStringWrapper> variables)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                string tmp = variables[i].ToString();
                this.stringBuilder.Append(tmp);
                this.PadWithChar(Formater.BaseColumnLength - tmp.Length, ' ');
            }

            this.Flush();

            for (int i = 0; i < this.ColumnCount; i++)
            {
                this.PadWithChar(Formater.BaseColumnLength, '-');
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

        /// <summary>
        /// Adds a given number of character to a string builder.
        /// </summary>
        /// <param name="count"> A number of characters. </param>
        /// <param name="c"> A character to add. </param>
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
}
