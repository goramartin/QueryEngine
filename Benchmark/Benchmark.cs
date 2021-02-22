using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueryEngine;
using System.Diagnostics;

namespace Benchmark
{
    class Mode
    {
        public List<string> groupers = new List<string>();
        public List<string> sorters = new List<string>();
        public QueryMode modeType;
    }

    class Normal : Mode
    {
        public Normal()
        {
            groupers.Add("refB");
            groupers.Add("refL");
            groupers.Add("globalB");
            groupers.Add("globalL");
            groupers.Add("localB");
            groupers.Add("localL");
            groupers.Add("twowayB");
            groupers.Add("twowayL");
            sorters.Add("mergeSort");

            modeType = QueryMode.Normal;
        }
    }

    class HalfStreamed : Mode
    {
        public HalfStreamed()
        {
            groupers.Add("twowayHSB");
            groupers.Add("twowayHSL");
            sorters.Add("abtreeHS");

            modeType = QueryMode.HalfStreamed;
        }
    }

    class Streamed : Mode
    {
        public Streamed()
        {
            groupers.Add("globalS");
            sorters.Add("abtreeS");
            modeType = QueryMode.Streamed;
        }
    }

    class Benchmark
    {
        static Stopwatch timer = new Stopwatch();
        static Mode[] modes = new Mode[]
        {
            new Normal(),
            new HalfStreamed(),
            new Streamed()
        };


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


        static void Main(string[] args)
        {
           


        }




    }
}
