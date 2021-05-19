using System;
using System.Collections.Generic;
using System.IO;

namespace QueryEngine
{

    /// <summary>
    /// A printer which prints results into a file.
    /// </summary>
    internal sealed class FilePrinter : Printer
    {
        /// <summary>
        /// Creates a file printer.
        /// </summary>
        /// <param name="rowFormat"> A format of a columns. </param>
        /// <param name="formater"> A type of formater. </param>
        /// <param name="fileName"> A file to print into. </param>
        public FilePrinter(List<ExpressionToStringWrapper> rowFormat, FormaterType formater, string fileName) : base(rowFormat)
        {
            if (!Formater.FileEndings.TryGetValue(formater, out string ending))
                throw new ArgumentException($"{this.GetType()}, file ending for given formater does not exist. Formater = {formater}");

            try
            {
                this.writer = File.AppendText(fileName + ending);
            }
            catch (IOException)
            {
                throw new IOException($"{this.GetType()}, failed to open file for writing. File name = {fileName}.");
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
