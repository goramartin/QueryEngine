using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueryEngine;

namespace Benchmark
{
    class Benchmark
    {
        static void Main(string[] args)
        {
            Console.WriteLine(args[0]);

            Graph graph = new Graph();
            Query query = Query.Create("n", "select x match (x);", graph, 1, "console", "simple", 1, null, "refL", "mergeSort", 4194304);
            query.Compute();

            Console.ReadLine();
        }
    }
}
