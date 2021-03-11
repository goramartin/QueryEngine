using System;
using System.Collections.Generic;
using System.Linq;
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

        // To measure solely search time + search time and result storing
        static List<string> matchQueries = new List<string>
        {
            "select count(*) match (x) -> (y) -> (z);",
            "select x match (x) -> (y) -> (z);",
            "select x, y match (x) -> (y) -> (z);",
            "select x, y, z match (x) -> (y) -> (z);",
        };
        static List<string> orderByQueries = new List<string>
        {
            "select y match (x) -> (y) -> (z) order by y;",
            "select y, x match (x) -> (y) -> (z) order by y, x;",
            "select x.PropTwo match (x) -> (y) -> (z) order by x.PropTwo;",
            "select x.PropThree match (x) -> (y) -> (z) order by x.PropThree;",
        };
        static List<string> groupByQueries = new List<string>
        {
            "select min(y.PropOne), avg(y.PropOne) match (x) -> (y) -> (z);",
            "select min(y.PropOne), avg(y.PropOne) match (x) -> (y) -> (z) group by y;",
            "select min(y.PropOne), avg(y.PropOne) match (x) -> (y) -> (z) group by y, x;",
            "select min(x.PropOne), avg(x.PropOne) match (x) -> (y) -> (z) group by x.PropTwo;",
            "select min(x.PropOne), avg(x.PropOne) match (x) -> (y) -> (z) group by x;",
            "select min(x.PropOne), avg(x.PropOne) match (x) -> (y) -> (z) group by x, y;",
            "select min(x.PropOne), avg(x.PropOne) match (x) -> (y) -> (z) group by x.PropOne;"
        };

        static int warmUps = 5;
        static int repetitions = 15;
        static int fixedArraySize = 4194304 * 2;
        static int threadCount = 8;
        static int verticesPerThread = 1024;
        static bool timeMatching = false;

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
            // Group by
            foreach (var mode in modes)
            {
                for (int i = 0; i < groupByQueries.Count; i++)
                {
                    foreach (var grouper in mode.groupers)
                    {
                        if (mode.GetType() == typeof(Normal))
                        {
                            if (threadCount == 1)
                            {
                                if (grouper != GrouperAlias.RefL && grouper != GrouperAlias.RefB)
                                    continue;
                            } else
                            {
                                if (grouper == GrouperAlias.RefB)
                                    continue;
                                if (grouper == GrouperAlias.RefL)
                                    continue;
                            }
                        }
                        Measure(mode.modeType, grouper, mode.baseSorter, groupByQueries[i], threadCount);
                    }
                }
            }

            // Order by 
            foreach (var mode in modes)
            {
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

                sw.WriteLine($" min = {min}  max = {max}  avg = {average}  meanDev = {meanDeviation}  relativeMeanDev = {relativeMeanDeviation}  sampleStdDev = {sampleStdDev}  stdDev = {stdDev}  nejistota = {nejistota} ");
                sw.WriteLine();
            }
        }

    }
}
