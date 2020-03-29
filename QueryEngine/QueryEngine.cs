using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace QueryEngine
{
    class QueryEngine
    {

        private static void Run(string[] args, TextReader reader, TextWriter writer)
        {
            //Every query needs valid SELECT and MATCH expr.
            //Every query must end with semicolon ';'.
            Graph graph = new Graph();
            
            while (true)
            {
                Console.WriteLine("Enter Query:");
                //Query query = new Query(reader, graph);
              
                Console.WriteLine();
                Console.WriteLine("Results:");

               // DFSPatternMatcher dfs = new DFSPatternMatcher(query.GetMatchPattern(), graph);
             //   dfs.Search();

                Console.WriteLine();
                Console.ReadLine();
                Console.WriteLine("Continue? y/n (single character answer):");
                string c;
                c= (Console.ReadLine());
                if (c[0] != 'y') break;
                Console.Clear();

            }
        }

        static void Main(string[] args)
        {
            /* try
             {
                if (QueryEngine.ThreadsPerQuery <= 0) 
                    throw new Exception("Cannot start a query with <= 0 threads.");
                Run(args, Console.In, Console.Out);
             }
             catch (Exception e )
             {
                 Console.WriteLine( e.Message);
             }
             */

            TestClass.RunTest();
            
        }


    }
}
