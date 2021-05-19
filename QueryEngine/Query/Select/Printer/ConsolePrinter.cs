using System;
using System.Collections.Generic;
using System.IO;

namespace QueryEngine 
{
    /// <summary>
    /// Printer which prints results into a console.
    /// </summary>
    internal sealed class ConsolePrinter : Printer
    {
        /// <summary>
        /// Creates a console printer.
        /// </summary>
        /// <param name="rowFormat"> A format of a columns. </param>
        /// <param name="formater"> A type of formater. </param>
        public ConsolePrinter(List<ExpressionToStringWrapper> rowFormat, FormaterType formater) : base(rowFormat)
        {
            try
            {
                this.writer = Console.Out;
            }
            catch (IOException)
            {
                throw new IOException($"{this.GetType()}, failed to open console for writing.");
            }
            this.formater = Formater.Factory(formater, rowFormat.Count, writer);

        }

        /// <summary>
        /// Flushed the formater buffer and releases resources of the writer.
        /// </summary>
        public override void Dispose()
        {
            this.formater.Flush();
            this.writer.Close();
        }
    }
}
