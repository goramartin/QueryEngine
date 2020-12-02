
/*! \file
This file includes definition of a Printer.
Printer is used by a select object to print results with appropriate format.
Printer holds a formater and manages printing of results to output.
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
    internal abstract class Printer : IDisposable, IRowPrinter
    {
        /// <summary>
        /// Contains valid printers.
        /// </summary>
        public static HashSet<string> Printers { get; }

        /// <summary>
        /// Variables that will compute values to be printed.
        /// </summary>
        protected List<ExpressionToStringWrapper> rowFormat;

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

        protected Printer(List<ExpressionToStringWrapper> rowFormat) : this()
        {
            if (rowFormat.Count <= 0) 
                throw new ArgumentException($"{this.GetType()}, was given empty header or row format.");

            this.rowFormat = rowFormat;
        }


        /// <summary>
        /// Prints row for one result.
        /// </summary>
        /// <param name="elements"> A one result from query search. </param>
        public void PrintRow(in TableResults.RowProxy elements)
        {
            for (int i = 0; i < this.rowFormat.Count; i++)
                this.formater.AddToFormat(this.rowFormat[i].GetValueAsString(elements));
        }

        public void PrintRow(in GroupByResultsList.GroupProxyList group)
        {
            for (int i = 0; i < this.rowFormat.Count; i++)
                this.formater.AddToFormat(this.rowFormat[i].GetValueAsString(group));
        }

        public void PrintRow(in GroupByResultsBucket.GroupProxyBucket group)
        {
            for (int i = 0; i < this.rowFormat.Count; i++)
                this.formater.AddToFormat(this.rowFormat[i].GetValueAsString(group));
        }

        public void PrintRow(in GroupByResultsArray.GroupProxyArray group)
        {
            for (int i = 0; i < this.rowFormat.Count; i++)
                this.formater.AddToFormat(this.rowFormat[i].GetValueAsString(group));
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
        public static Printer Factory(string printerType, List<ExpressionToStringWrapper> rowFormat, string formater, string fileName= null)
        {
            if (printerType == "console")
                return new ConsolePrinter(rowFormat, formater);
            else if (printerType == "file")
                return new FilePrinter(rowFormat, formater, fileName);
            else throw new ArgumentException($"Printer factory, printer type does not exist. Printer = {printerType}.");
        }

        public abstract void Dispose();

    }


}
