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
        public FilePrinter(List<PrinterVariable> rowFormat, string formater, string fileName) : base(rowFormat)
        {
            if (!Formater.fileEndings.TryGetValue(formater, out string ending)) 
                throw new ArgumentException($"{this.GetType()}, file ending for given formater does not exist. Formater = {formater}");  

            try
            {
                this.writer = new StreamWriter(fileName + ending);
            }
            catch (IOException)
            {
                throw new IOException($"{this.GetType()}, failed to open file for writing. File name = {fileName}.");
            }
            this.formater = Formater.FormaterFactory(formater, rowFormat.Count, writer);

        }

        public override void Dispose()
        {
            this.formater.Flush();
            this.writer.Close();
        }
    }

}
