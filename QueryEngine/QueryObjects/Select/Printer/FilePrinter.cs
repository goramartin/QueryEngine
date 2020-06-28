/*! \file
   
    This file includes definition of a file printer.
    The file printer prints results into a file.
    The file name is given in one of the program arguments.

    If multiple queries are computed during program runtime, the printer
    always appends results into the same file.

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
    /// Printer which prints results into a file.
    /// </summary>
    internal sealed class FilePrinter : Printer
    {
        /// <summary>
        /// Creates a file printer.
        /// </summary>
        /// <param name="rowFormat"> Format of a columns. </param>
        /// <param name="formater"> Type of formater. </param>
        /// <param name="fileName"> File to print into. </param>
        public FilePrinter(List<PrintVariable> rowFormat, string formater, string fileName) : base(rowFormat)
        {
            if (!Formater.fileEndings.TryGetValue(formater, out string ending))
                throw new ArgumentException($"{this.GetType()}, file ending for given formater does not exist. Formater = {formater}");

            try
            {
                this.writer = File.AppendText(fileName + ending);
            }
            catch (IOException)
            {
                throw new IOException($"{this.GetType()}, failed to open file for writing. File name = {fileName}.");
            }
            this.formater = Formater.FormaterFactory(formater, rowFormat.Count, writer);

        }

        /// <summary>
        /// Flushed formater buffer and releases resources of writer.
        /// </summary>
        public override void Dispose()
        {
            this.formater.Flush();
            this.writer.Close();
        }
    }
}
