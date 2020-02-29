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
            Graph graph = new Graph(args);
            
            while (true)
            {
                Console.WriteLine("Enter Query:");
                Query query = new Query(reader, graph.NodeTables, graph.EdgeTables);
              
                Console.WriteLine();
                Console.WriteLine("Results:");

                DFSPatternMatcher dfs = new DFSPatternMatcher(query.GetMatchPattern(), graph);
                dfs.Search();

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
               Run(args, Console.In, Console.Out);
            }
            catch (Exception e )
            {
                Console.WriteLine( e.Message);
                Console.ReadLine();
                Console.ReadLine();
            }
            */
            TestClass.Run();
            
        }


    }
}
