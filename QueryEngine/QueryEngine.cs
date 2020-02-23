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
        private static Query CreateQuery(TextReader reader,
            Dictionary<string, Table> v, Dictionary<string, Table> e) 
        {
            //Create tokens from console.
            List<Token> tokens = Tokenizer.Tokenize(reader);

            //Parse and create in order of the query words.
            Parser.ResetPosition();
            Scope scope = new Scope();
            SelectObject select = CreateSelectObject(tokens);
            MatchObject match = CreateMatchObject(tokens, scope, v, e);


            //Check if it successfully parsed every token.
            if (tokens.Count != Parser.GetPosition()) 
                throw new ArgumentException("Failed to parse every token."); 

            Query query = new Query(select, match, scope);

            return query;
        }

        private static SelectObject CreateSelectObject(List<Token> tokens)
        {
            SelectNode selectNode = Parser.ParseSelectExpr(tokens);
            SelectVisitor visitor = new SelectVisitor();
            selectNode.Accept(visitor);
            SelectObject so = new SelectObject(visitor.GetResult());
            return so;
        }

        private static MatchObject CreateMatchObject(List<Token> tokens, Scope s, 
            Dictionary<string, Table> v, Dictionary<string, Table> e)
        {
            //  MatchNode matchNode = Parser.ParseMatchExpr(tokens);
            //  MatchVisitor visitor = new MatchVisitor(s,v,e);
            //  matchNode.Accept(visitor);
            //  MatchObject mo = new MatchObject(visitor.GetResult());
            //  return mo;
            return null;
        }

        private static Graph CreateGraph(string[] args)
        {
            Graph g = new Graph();
            g.LoadNodeTables("NodeTypes.txt");
            g.LoadEdgeTables("EdgeTypes.txt");
            g.LoadVertices("Nodes.txt");
            g.LoadEdges("Edges.txt");
            return g;
        }


        private static void Run(string[] args, TextReader reader, TextWriter writer)
        {
            //Every query needs valid SELECT and MATCH expr.
            //Every query must end with semicolon ';'.
            Graph g = CreateGraph(args);
            
            while (true)
            {
                Console.WriteLine("Enter Query:");
                Query query = CreateQuery(reader, g.NodeTables, g.EdgeTables);
                if (!query.CheckCorrectnessOfQuery()) throw new ArgumentException("Query is not correct, check assigned variables and their types.");
                Console.WriteLine();
                Console.WriteLine("Results:");

                DFSPatternMatcher dfs = new DFSPatternMatcher(query.GetMatchPattern(),g);
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
