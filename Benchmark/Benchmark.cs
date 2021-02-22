using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using QueryEngine;
using System.Diagnostics;

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
            if (threadCount == 1)
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }

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
            
            long[] times = new long[repetitions];
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

            Console.WriteLine();
            Console.WriteLine($"Starting processing of measurements for {mode} / {gA} / {sA} / {query} / {threadCount}.");
            
            ProcessResults(times);
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

        static void ProcessResults(long[] items)
        {
            long max = items.Max();
            long min = items.Min();
            double average = items.Average();

            // Create average deviation.
            double meanDeviation = CalculateMeanOfDeviations(items, average);

            double relativeMeanDeviation = (meanDeviation / average) * (100);

            // The result is -> x = (average) +- (meanDeviation) with relative error relativeMeanDeviation (%).
            // include max a min ve vypisu.
        }

        static double CalculateMeanOfDeviations(long[] items, double average)
        {
            double[] deviations = new double[items.Length];
            for (int i = 0; i < items.Length; i++)
                deviations[i] = average - (double)items[i];

            return ( (deviations.Sum(t => Math.Abs(t))) / deviations.Length); 
        }
    }
}
