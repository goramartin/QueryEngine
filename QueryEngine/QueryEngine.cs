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
            Dictionary<string, Table> nodeTables, Dictionary<string, Table> edgeTables) 
        {
            // Create tokens from console.
            List<Token> tokens = Tokenizer.Tokenize(reader);

            // Parse and create in order of the query words.
            Parser.ResetPosition();
            Scope scope = new Scope();
            SelectObject select = CreateSelectObject(tokens);
            MatchObject match = CreateMatchObject(tokens, scope, nodeTables, edgeTables);

            // Check correctness of select part
            select.CheckCorrectnessOfSelect(nodeTables, edgeTables, scope);


            // Check if it successfully parsed every token.
            if (tokens.Count != Parser.GetPosition()) 
                throw new ArgumentException("Failed to parse every token."); 

            Query query = new Query(select, match, scope);

            return query;
        }

        private static SelectObject CreateSelectObject(List<Token> tokens)
        {
            // Create tree of select part of query
            SelectNode selectNode = Parser.ParseSelectExpr(tokens);
            
            // Process parse tree and create list of variables to be printed
            SelectVisitor visitor = new SelectVisitor();
            selectNode.Accept(visitor);
            
            SelectObject selectObject = new SelectObject(visitor.GetResult());
            return selectObject;
        }

        private static MatchObject CreateMatchObject(List<Token> tokens, Scope scope, 
            Dictionary<string, Table> nodeTables, Dictionary<string, Table> edgeTables)
        {
            // Create parse tree of match part of query and
            // create a shallow pattern
            MatchNode matchNode = Parser.ParseMatchExpr(tokens);
            MatchVisitor matchVisitor = new MatchVisitor(nodeTables, edgeTables);
            matchNode.Accept(matchVisitor);

            //Create real pattern and scope
            MatchObject matchObject = new MatchObject();
            matchObject.CreatePattern(matchVisitor.GetResult(), scope);

            return matchObject;
        }

        private static Graph CreateGraph(string[] args)
        {
            Graph graph = new Graph();
            graph.LoadNodeTables("NodeTypes.txt");
            graph.LoadEdgeTables("EdgeTypes.txt");
            graph.LoadVertices("Nodes.txt");
            graph.LoadEdges("Edges.txt");
            return graph;
        }


        private static void Run(string[] args, TextReader reader, TextWriter writer)
        {
            //Every query needs valid SELECT and MATCH expr.
            //Every query must end with semicolon ';'.
            Graph graph = CreateGraph(args);
            
            while (true)
            {
                Console.WriteLine("Enter Query:");
                Query query = CreateQuery(reader, graph.NodeTables, graph.EdgeTables);
              
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
