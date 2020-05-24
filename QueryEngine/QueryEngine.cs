
/*! \file
    
    This file is an entry point of a program.
    Contains static class of a query engine and provides a simple api for user defined queries.
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
    /// Entry point of a program.
    /// Class represents main algorithm loop where user inputs queries and subsequently they are computed
    /// and printed.
    /// </summary>
    sealed class QueryEngine
    {

        /// <summary>
        /// Parses argument that expects to be a thread count.
        /// </summary>
        /// <param name="param"> Application argument. </param>
        /// <returns> Thread count.</returns>
        private static int GetThreadCount(string param)
        {
            if (!Int32.TryParse(param, out int threadCount))
                throw new ArgumentException("Failed to parse thread count.");
            else if (threadCount <= 0) 
                throw new ArgumentException("Thread count cannot be negative.");
                    else return threadCount;
        }

        /// <summary>
        /// Parses argument that expects to be a vertices per round.
        /// </summary>
        /// <param name="param"> Application argument. </param>
        /// <returns> Vertices per round.</returns>
        private static int GetVerticesPerhread(string param)
        {
            if (!Int32.TryParse(param, out int verticesPerThread))
                throw new ArgumentException("Failed to parse vertices per thread.");
            else if (verticesPerThread <= 0)
                throw new ArgumentException("Vertices per thread cannot be negative.");
            else return verticesPerThread;
        }

        /// <summary>
        /// Parses argument that expects to be a printer type.
        /// </summary>
        /// <param name="param"> Application argument.</param>
        /// <returns> Printer type. </returns>
        private static string GetPrinter(string param)
        {

            if (!Printer.Printers.Contains(param))
                throw new ArgumentException("Inputed printer that does not exists.");
            else return param;
        }

        /// <summary>
        /// Parses argument that expects to be a formater type.
        /// </summary>
        /// <param name="param"> Application argument.</param>
        /// <returns> Formater type. </returns>
        private static string GetFormater(string param)
        {
            if (!Formater.Formaters.Contains(param))
                throw new ArgumentException("Inputed printer that does not exists.");
            else return param;
        }

        /// <summary>
        /// Parses argument that expects to be a file name.
        /// </summary>
        /// <param name="pars"> Application arguments.</param>
        /// <returns> File name. </returns>
        private static string GetFileName(string[] pars)
        {
            if (pars[2] == "file")
            {
                if (pars.Length < 5) throw new ArgumentException("Missing file name.");
                else return pars[4];
            }
            else return null; 
        }

    /// <summary>
    /// Main algorith.
    /// Infinite loop when user inputs queries which are subsequently computed and results are printed.
    /// </summary>
    /// <param name="args"> Program arguments.</param>
    /// <param name="reader"> Reader from which to read input. </param>
    private static void Run(string[] args, TextReader reader)
        {
            if (args.Length < 4) throw new ArgumentException("Wrong number of program parameters.");

            // Parse program arguments
            int ThreadCount = GetThreadCount(args[0]);
            int VerticesPerThread = GetVerticesPerhread(args[1]);
            string Printer = GetPrinter(args[2]);
            string Formater = GetFormater(args[3]);
            string FileName = GetFileName(args);
            
            // Load graph.
            Graph graph = new Graph();
            
            //Every query needs valid SELECT and MATCH expr.
            //Every query must end with semicolon ';'.
            while (true)
            {
                Console.WriteLine("Enter Query:");
                Query query = new Query(reader, graph, ThreadCount,VerticesPerThread
                                        ,Printer,Formater, FileName);
                Console.WriteLine();
                query.ComputeQuery();
                Console.WriteLine("Finished computing. Pres enter to continue...");
                Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine("Continue with another query? y/n (single character answer):");
                string c;
                c= (Console.ReadLine());
                if (c[0] != 'y') break;
                Console.Clear();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Run(args, Console.In);
            }
            catch (Exception e )
            {
                Console.WriteLine(e);
                Console.WriteLine("Press enter to close the application");
                Console.ReadLine();
            }

          //  TestClass.RunTest();
        }


        public static int GetSizeOfObject(object obj, int avgStringSize = -1)
        {
            int pointerSize = IntPtr.Size;
            int size = 0;
            Type type = obj.GetType();
            var info = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var field in info)
            {
                if (field.FieldType.IsValueType)
                {
                    size += System.Runtime.InteropServices.Marshal.SizeOf(field.FieldType);
                }
                else
                {
                    size += pointerSize;
                    if (field.FieldType.IsArray)
                    {
                        var array = field.GetValue(obj) as Array;
                        if (array != null)
                        {
                            var elementType = array.GetType().GetElementType();
                            if (elementType.IsValueType)
                            {
                                size += System.Runtime.InteropServices.Marshal.SizeOf(field.FieldType) * array.Length;
                            }
                            else
                            {
                                size += pointerSize * array.Length;
                                if (elementType == typeof(string) && avgStringSize > 0)
                                {
                                    size += avgStringSize * array.Length;
                                }
                            }
                        }
                    }
                    else if (field.FieldType == typeof(string) && avgStringSize > 0)
                    {
                        size += avgStringSize;
                    }
                }
            }
            return size;
        }




    }
}
