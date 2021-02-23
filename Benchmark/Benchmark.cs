using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using QueryEngine;
using System.Diagnostics;
using System.IO;

namespace Benchmark
{
    class Benchmark
    {
        static Stopwatch timer = new Stopwatch();
        static Mode[] modes = new Mode[]
        {
            new Normal(),
            new HalfStreamed(),
            new Streamed()
        };
        static Graph graph;
        static string fileName = "results.txt";

        static List<string> matchQueries = new List<string>
        {



        };
        static List<string> orderByQueries = new List<string>
        {



        };
        static List<string> groupByQueries = new List<string>
        {



        };

        static int warmUps = 5;
        static int repetitions = 15;
        static int fixedArraySize = 4194304;
        static int threadCount = 1;
        static int verticesPerThread = 512;
        static bool timeMatching = true;

        static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            //if (threadCount == 1)
            //{
            //    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
            //    Thread.CurrentThread.Priority = ThreadPriority.Highest;
            //}

            graph = new Graph();
            CleanGC();

            if (timeMatching)
            {
                MeasureMatching();
            } else
            {
                MeasureAggregates();
            }
        }

        static void MeasureMatching()
        {
            for (int i = 0; i < matchQueries.Count; i++)
            {
                Measure(QueryMode.Normal, GrouperAlias.RefL, SorterAlias.MergeSort, matchQueries[i], threadCount);
            }
        }

        static void MeasureAggregates()
        {
            foreach (var mode in modes)
            {
                // Group by
                for (int i = 0; i < groupByQueries.Count; i++)
                {
                    foreach (var grouper in mode.groupers)
                    {
                        Measure(mode.modeType, grouper, mode.baseSorter, groupByQueries[i], threadCount);
                    }
                }

                // Order by 
                for (int i = 0; i < orderByQueries.Count; i++)
                {
                    foreach (var sorter in mode.sorters)
                    {
                        Measure(mode.modeType, mode.baseGrouper, sorter, orderByQueries[i], threadCount);
                    }
                }
            }
        }

        static void Measure(QueryMode mode, GrouperAlias gA, SorterAlias sA, string query, int threadCount)
        {
            WarmUp(mode, gA, sA, query, threadCount);

            Console.WriteLine();
            Console.WriteLine($"Starting measurements for {mode} / {gA} / {sA} / {query} / {threadCount}.");
            
            double[] times = new double[repetitions];
            for (int i = 0; i < repetitions; i++)
            {
                CleanGC();
                
                var q = Query.Create(mode, query, graph, threadCount, PrinterType.Console, FormaterType.Simple, verticesPerThread, null, gA, sA, fixedArraySize, false);

                timer.Restart();

                q.Compute();

                timer.Stop();
                times[i] = timer.ElapsedMilliseconds;

                Console.WriteLine($"Finished repetition {i} with {times[i]}");
            }

            ProcessResults(times, $"Measurements for {mode} / {gA} / {sA} / {query} / {threadCount}.");
        }

        static void WarmUp(QueryMode mode, GrouperAlias gA, SorterAlias sA, string query, int threadCount)
        {
            Console.WriteLine($"Starting warm ups for {mode} / {gA} / {sA} / {query} / {threadCount}.");
            for (int i = 0; i < warmUps; i++)
            {
                CleanGC();

                var q = Query.Create(mode, query, graph, threadCount, PrinterType.Console, FormaterType.Simple, verticesPerThread, null, gA, sA, fixedArraySize, false);
                q.Compute();

                Console.WriteLine($"Finished warm up no. {i}.");
            }
        }

        static void CleanGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        static void ProcessResults(double[] items, string messege)
        {
            double max = items.Max();
            double min = items.Min();
            double average = items.Average();

            double meanDeviation = (items.Sum(t => Math.Abs(t - average)) / items.Length);
            double relativeMeanDeviation = ((meanDeviation / average) * (100));

            double squareSum = items.Sum(t => (t - average) * (t - average));
            double sampleStdDev = Math.Sqrt(squareSum / (items.Length - 1));
            double stdDev = Math.Sqrt(squareSum / items.Length);

            double nejistota = Math.Sqrt(squareSum / (items.Length * (items.Length - 1)));

            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(fileName))
            {
                sw.WriteLine(messege);
                for (int i = 0; i < items.Length; i++)
                    sw.WriteLine(items[i]);

                sw.WriteLine($" min = {min}  max = {max}  avg = {average}  meanDev = {meanDeviation}  relativeMeanDev = {relativeMeanDeviation}");
                sw.WriteLine($" sampleStdDev = {sampleStdDev}  stdDev = {stdDev}  nejistota = {nejistota} ");
            }
        }

    }
}
