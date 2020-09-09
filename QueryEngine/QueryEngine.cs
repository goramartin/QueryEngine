
/*! \file
    
    This file is an entry point of a program.
    Contains static class of a query engine and provides a simple api for user defined queries.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QueryEngine
{
    

    /// <summary>
    /// Entry point of a program.
    /// Class represents main algorithm loop where user inputs queries and subsequently they are computed
    /// and printed.
    /// </summary>
    sealed class QueryEngine
    {
        public static Stopwatch stopwatch = new Stopwatch();

        public static void PrintElapsedTime()
        {
            TimeSpan ts = QueryEngine.stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Elapsed: " + elapsedTime);
        }
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
        /// Parses argument that expects to be a vertices per round.
        /// </summary>
        /// <param name="threadCount"> Number of threads to use for the app. </param>
        /// <param name="args"> Application argument. </param>
        /// <returns> Vertices per round.</returns>
        private static int GetVerticesPerhread(int threadCount, string[] args)
        {
            if (threadCount == 1) return 1;
            else if (args.Length < 4)
                throw new ArgumentException("Missing number of vertices per thread in the argument list.");
            else if (!Int32.TryParse(args[3], out int verticesPerThread))
                throw new ArgumentException("Failed to parse vertices per thread.");
            else if (verticesPerThread <= 0)
                throw new ArgumentException("Vertices per thread cannot be negative.");
            else return verticesPerThread;
        }

        /// <summary>
        /// Parses argument that expects to be a file name.
        /// </summary>
        /// <param name="threadCount"> Number of threads fro the app.</param>
        /// <param name="printer"> The destination of printer. </param>
        /// <param name="args">Application arguments.</param>
        /// <returns> File name. </returns>
        private static string GetFileName(int threadCount, string printer, string[] args)
        {
            // If it runs in parallel, the number of vertices precedes the file name
            if (printer == "file")
            {
                if (threadCount == 1 && args.Length == 4) return args[3];
                else if (args.Length == 5) return args[4];
                else throw new ArgumentException("Missing file name.");
            }
            else return null; 
        }

        /// <summary>
        /// Parses program arguments.
        /// </summary>
        /// <param name="args"> Program arguments. </param>
        /// <returns> Returns query execution helper actualised with parsed arguments. </returns>
        private static QueryExecutionHelper ParseProgramArguments(string[] args)
        {
            QueryExecutionHelper qEHelper = new QueryExecutionHelper();
            qEHelper.ThreadCount = GetThreadCount(args[0]);
            qEHelper.Printer = GetPrinter(args[1]);
            qEHelper.Formater =  GetFormater(args[2]);
            qEHelper.VerticesPerThread = GetVerticesPerhread(qEHelper.ThreadCount, args);
            qEHelper.FileName =  GetFileName(qEHelper.ThreadCount, qEHelper.Printer, args);
            return qEHelper;
        }

        /// <summary>
        /// Awaits an user to input answer whether the user wants to input another query.
        /// </summary>
        /// <returns> True if user wants to continue. Otherwise, false. </returns>
        private static bool ContinueWithAnotherQuery()
        {
            Console.WriteLine();
            Console.WriteLine("Do you want to continue with another query? y/n (single character answer):");
            Console.WriteLine();

            string unfinishedLine = Console.ReadLine();
            string userValue = Console.ReadLine(); 
            
            if (userValue[0] != 'y') return false;
            else return true;
        }

    /// <summary>
    /// Main algorith.
    /// Infinite loop when user inputs queries which are subsequently computed and results are printed.
    /// </summary>
    /// <param name="args"> Program arguments.</param>
    /// <param name="reader"> Reader from which to read input. </param>
    private static void Run(string[] args, TextReader reader)
        {
           // if (args.Length < 3) throw new ArgumentException("Wrong number of program parameters.");
            QueryExecutionHelper qEHelper = ParseProgramArguments(args);

            // Set only if on a desktop machine
            using (Process p = Process.GetCurrentProcess())
            p.PriorityClass = ProcessPriorityClass.RealTime; //High;

            // Set a number of threads in the thread pool that will be immediatelly spawned on demand,
            // without the need to wait.
            if (qEHelper.ThreadCount != 1)
                ThreadPool.SetMinThreads(qEHelper.ThreadCount, 0);

            // Load the graph.
            Graph graph = new Graph();
            Console.Clear();

            // Main loop of a program.
            // Program awaits users input query and computes it.
            // After computation, the user is promted to choose whether he wants to compute another query or close the app.
            while (true)
            {
                Console.WriteLine("Enter Query:");
                try
                {
                    Console.WriteLine();
                    Query query = new Query(reader, graph, qEHelper);
                    Console.WriteLine();

                    query.ComputeQuery();
                   
                    stopwatch.Stop();
                    PrintElapsedTime();
                    stopwatch.Reset();

                    Console.WriteLine("Finished the computation of the query.");
                    if (!ContinueWithAnotherQuery()) return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (!ContinueWithAnotherQuery()) return;
                    stopwatch.Reset();
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
               Run(args, Console.In);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to close the application.");
                Console.ReadLine();
            }

        }
    }
}
