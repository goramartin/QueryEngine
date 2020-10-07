/*! \file 
This file includes definition of a mark down formater.
Prints header and values into a markd down table format.
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
    /// Format is printed as a markdown table.
    /// </summary>
    internal sealed class MarkDownFormater : Formater
    {
        public MarkDownFormater(int columnCount, TextWriter writer) : base(columnCount, writer) { }


        /// <summary>
        /// Adds word to a format and separates it with a | character to end a column in markdown syntax.. 
        /// </summary>
        /// <param name="word"> Word to add to a format. </param>
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
        /// header and results. Columns are also | separated on sides.
        /// </summary>
        /// <param name="variables"> Header format. </param>
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
        /// Flushed string builder and prepares for printing next row.
        /// </summary>
        public override void Flush()
        {
            this.writer.WriteLine(this.stringBuilder.ToString());
            this.stringBuilder.Clear();
            this.ColumnsFilled = 0;
        }
    }
}
