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
            //to do better if it returns null when failed.
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
            MatchNode matchNode = Parser.ParseMatchExpr(tokens);
            MatchVisitor visitor = new MatchVisitor(s,v,e);
            matchNode.Accept(visitor);
            MatchObject mo = new MatchObject(visitor.GetResult());
            return mo;
        }

        private static Graph CreateGraph(string[] args)
        {
            //better if it returns null when failed.
            Graph g = new Graph();
            g.LoadNodeTables("VertexTypes.txt");
            g.LoadEdgeTables("EdgeTypes.txt");
            g.LoadEdgeList("NodesEdges.txt");
            return g;
        }


        private static void Run(string[] args, TextReader reader, TextWriter writer)
        {


            Graph g = CreateGraph(args);
            Query query = CreateQuery(reader, g.NodeTables, g.EdgeTables);    








        }

        static void Main(string[] args)
        {
            try
            {
         //      Run(args, Console.In, Console.Out);
           //     return;
            }
            catch (Exception e )
            {
                Console.WriteLine( e.Message);
            }

            Graph g = new Graph();
            g.LoadNodeTables("VertexTypes.txt");
            g.LoadEdgeTables("EdgeTypes.txt");
            g.LoadEdgeList("NodesEdges.txt");
            Scope scope = new Scope();
            List<Token> tokens = Tokenizer.Tokenize(Console.In);

            foreach (var item in tokens)
            {
                Console.WriteLine(item.type);
                if (item.type == Token.TokenType.Identifier) Console.WriteLine(item.strValue) ;
            }

         
            SelectNode d = Parser.ParseSelectExpr(tokens);
            MatchNode s = Parser.ParseMatchExpr(tokens);
            SelectVisitor selectVisitor = new SelectVisitor();
            MatchVisitor matchVisitor = new MatchVisitor(scope,g.NodeTables, g.EdgeTables);
            
            d.Accept(selectVisitor);
            var k = selectVisitor.GetResult();
            s.Accept(matchVisitor);
            var l = matchVisitor.GetResult();

            Query q = new Query(new SelectObject(k), new MatchObject(l), scope);
            Console.WriteLine(q.CheckCorrectnessOfScope());



            Console.ReadLine();

            

            /////////////
            Console.WriteLine();
            //Display whats inside dictionary of nodes 
            foreach (var item in g.NodeTables)
            {
                Console.WriteLine(item.Key);
                Console.WriteLine(item.Value.IRI);
                foreach (var ite in item.Value.properties)
                {
                    Console.WriteLine(ite.IRI);
                    Console.WriteLine(ite.GetType());
                }

                Console.WriteLine();
            }
            Console.WriteLine();




            /////////////
            
            //Display whats inside dictionary of edges
            foreach (var item in g.EdgeTables)
            {
                Console.WriteLine(item.Key);
                Console.WriteLine(item.Value.IRI);
                foreach (var ite in item.Value.properties)
                {
                    Console.WriteLine(ite.IRI);
                    Console.WriteLine(ite.GetType());
                }

                Console.WriteLine();
            }
            Console.WriteLine() ;

            ///

            
            /////////////
            
            //Display whats inside vertices
            foreach (var item in g.vertices)
            {
                Console.WriteLine(item.id);
                Console.WriteLine(item.table.IRI);
                Console.WriteLine(item.edgePosition);
                Console.WriteLine();
                foreach (var it in item.incomingEdges)
                {
                    Console.WriteLine(it.FromVertex.id);
                    Console.WriteLine(it.incomingEdge.id);
                }
            }

            //displey whats inside edges
            foreach (var item in g.edges)
            {
                Console.WriteLine(item.id);
                Console.WriteLine(item.table.IRI);
                Console.WriteLine(item.endVertex.id);
                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}
