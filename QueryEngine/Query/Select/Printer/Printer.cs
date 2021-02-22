﻿/*! \file
This file includes definition of a Printer.
Printer is used by a select object to print results with appropriate format.
Printer holds a formater and manages printing of results to output.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace QueryEngine
{
    public enum PrinterType { File, Console }
    /// <summary>
    /// Abstract class for printing results.
    /// </summary>
    internal abstract class Printer : IDisposable, IRowPrinter
    {

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

        public void PrintRow(in AggregateBucketResult[] group)
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
        public static Printer Factory(PrinterType printerType, List<ExpressionToStringWrapper> rowFormat, FormaterType formater, string fileName= null)
        {
            if (printerType == PrinterType.Console)
                return new ConsolePrinter(rowFormat, formater);
            else if (printerType == PrinterType.File)
                return new FilePrinter(rowFormat, formater, fileName);
            else throw new ArgumentException($"Printer factory, printer type does not exist. Printer = {printerType}.");
        }

        public abstract void Dispose();
    }


}
