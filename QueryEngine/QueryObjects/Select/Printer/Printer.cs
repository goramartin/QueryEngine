
/*! \file
  
    This file includes definition of a Printer.
    Printer is used by a select object to print results with appropriate format.
    Printer holds a formater and manages printing of results.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Abstract class for printing results.
    /// </summary>
    abstract class Printer : IDisposable
    {
        /// <summary>
        /// Contains valid printers.
        /// </summary>
        public static HashSet<string> Printers;

        /// <summary>
        /// Defines how each row will look like. Each variable is one column which is equivalent to one value in a row.
        /// </summary>
        protected List<PrinterVariable> rowFormat;
        /// <summary>
        /// Defines what resulting table will look like.
        /// </summary>
        protected Formater formater;
        /// <summary>
        /// Defines where the printing will be done.
        /// </summary>
        protected TextWriter writer;

        /// <summary>
        /// Inicialises static dictionary of printer types.
        /// </summary>
        static Printer()
        {
            Printers = new HashSet<string>();
            Printers.Add("console");
            Printers.Add("file");
        }

        protected Printer()
        {
            this.rowFormat = null;
            this.formater = null;
            this.writer = null;
        }

        protected Printer(List<PrinterVariable> rowFormat) : this()
        {
            if (rowFormat.Count <= 0) 
                throw new ArgumentException($"{this.GetType()}, was given empty header or row format.");

            this.rowFormat = rowFormat;
        }


        /// <summary>
        /// Prints row for one result.
        /// </summary>
        /// <param name="elements"> A one result from query search. </param>
        public void PrintRow( Element[] elements)
        {
            for (int i = 0; i < this.rowFormat.Count; i++)
            { 
                var tmpElement = elements[this.rowFormat[i].VariableIndex];
                
                // Access correct element and get its string value.
                var tmpStrProp = this.rowFormat[i].GetSelectVariableAsString(tmpElement);
                this.formater.AddToFormat(tmpStrProp);
            }
        }

        /// <summary>
        /// Prints entire header of a table.
        /// </summary>
        public void PrintHeader()
        {
            this.formater.FormatHeader(this.rowFormat);
        }

       
        /// <summary>
        /// Factory for printer class.
        /// </summary>
        /// <param name="printerType"> Printer type. </param>
        /// <param name="rowFormat"> Format of a columns. </param>
        /// <param name="formater"> Formater type. </param>
        /// <param name="fileName"> File name if defined file printer. </param>
        /// <returns> Printer instance. </returns>
        public static Printer PrinterFactory(string printerType, List<PrinterVariable> rowFormat, string formater, string fileName= null)
        {
            if (printerType == "console")
                return new ConsolePrinter(rowFormat, formater);
            else if (printerType == "file")
                return new FilePrinter(rowFormat, formater, fileName);
            else throw new ArgumentException($"Printer factory, printer type does not exist. Printer = {printerType}.");
        }

        public abstract void Dispose();
    }

    /// <summary>
    /// Printer which prints results into a console.
    /// </summary>
    class ConsolePrinter : Printer
    {
        /// <summary>
        /// Creates a console printer.
        /// </summary>
        /// <param name="rowFormat"> Format of a columns. </param>
        /// <param name="formater"> Type of formater. </param>
        public ConsolePrinter( List<PrinterVariable> rowFormat, string formater) : base(rowFormat)
        {
            try
            {
                this.writer = Console.Out;
            }
            catch (IOException)
            {
                throw new IOException($"{this.GetType()}, failed to open console for writing.");
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

    /// <summary>
    /// Printer which prints results into a file.
    /// </summary>
    class FilePrinter : Printer
    {
        /// <summary>
        /// Creates a file printer.
        /// </summary>
        /// <param name="rowFormat"> Format of a columns. </param>
        /// <param name="formater"> Type of formater. </param>
        /// <param name="fileName"> File to print into. </param>
        public FilePrinter(List<PrinterVariable> rowFormat, string formater, string fileName) : base(rowFormat)
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
