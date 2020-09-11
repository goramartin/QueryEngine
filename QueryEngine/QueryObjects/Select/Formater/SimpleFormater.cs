/*! \file
This file includes definition of a simple formater which is used by a printer.
Simple formater formats output columns only with defined number od spaces.
Header and values are separated only by a line of dashes.
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
    /// Simple formater prints output with normalised length of a one column in a row.
    /// No table and separators are only spaces.
    /// </summary>
    internal sealed class SimpleFormater : Formater
    {
        public SimpleFormater(int columnCount, TextWriter writer) : base(columnCount, writer) { }

        /// <summary>
        /// Adds word to a format and separates it with a space character. 
        /// </summary>
        /// <param name="word"> Word to add to a format. </param>
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

            for (int i = 0; i < this.ColumnCount; i++)
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
            this.ColumnsFilled = 0;
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
}
