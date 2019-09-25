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
            MatchNode matchNode = Parser.ParseMatchExpr(tokens);
            MatchVisitor visitor = new MatchVisitor(s,v,e);
            matchNode.Accept(visitor);
            MatchObject mo = new MatchObject(visitor.GetResult());
            return mo;
        }

        private static Graph CreateGraph(string[] args)
        {
            Graph g = new Graph();
            g.LoadNodeTables("VertexTypes.txt");
            g.LoadEdgeTables("EdgeTypes.txt");
            g.LoadEdgeList("NodesEdges.txt");
            return g;
        }


        private static void Run(string[] args, TextReader reader, TextWriter writer)
        {
            //Every query needs valid SELECT and MATCH expr.
            //Every query must end with semicolon ';'.
            Graph g = CreateGraph(args);
            Query query = CreateQuery(reader, g.NodeTables, g.EdgeTables);
            if (!query.CheckCorrectnessOfQuery()) throw new ArgumentException("Query is not correct, check assigned variables and their types.");
            DFSPatternMatcher dfs = new DFSPatternMatcher(query.GetMatchPattern(), g);
            dfs.Search();
        }

        static void Main(string[] args)
        {
            try
            {
               //Run(args, Console.In, Console.Out);
               //return;
            }
            catch (Exception e )
            {
                Console.WriteLine( e.Message);
            }

            #region PRINT

            
            Graph g = new Graph();
            g.LoadNodeTables("VertexTypes.txt");
            g.LoadEdgeTables("EdgeTypes.txt");
            g.LoadEdgeList("NodesEdges.txt");
          
            
            
            
            //just for testing
            ///////////////////////////////////////
            
            
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

            Console.ReadLine();
            /*
            Query q = new Query(new SelectObject(k), new MatchObject(l), scope);
            Console.WriteLine(q.CheckCorrectnessOfQuery());
            Console.WriteLine();
            DFSPatternMatcher dfs = new DFSPatternMatcher(l, g);
            dfs.Search();



            Console.ReadLine();




            /////////////
            Console.WriteLine();
            //Display whats inside dictionary of nodes 
            foreach (var item in g.NodeTables)
            {
                Console.WriteLine("Key:"+item.Key);
                Console.WriteLine("TableIri:"+item.Value.IRI);
                foreach (var ite in item.Value.properties)
                {
                    Console.WriteLine("PropertyIRI:"+ite.IRI);
                    Console.WriteLine("PropertyType:"+ ite.GetType());
                }

                Console.WriteLine();
            }
            Console.WriteLine();




            /////////////
            
            //Display whats inside dictionary of edges
            foreach (var item in g.EdgeTables)
            {
                Console.WriteLine("Key:" + item.Key);
                Console.WriteLine("TableIri:" + item.Value.IRI);
                foreach (var ite in item.Value.properties)
                {
                    Console.WriteLine("PropertyIRI:" + ite.IRI);
                    Console.WriteLine("PropertyType:" + ite.GetType());
                }

                Console.WriteLine();
            }
            Console.WriteLine() ;

            ///


            /////////////

            Console.WriteLine("Vertices");
            //Display whats inside vertices
            foreach (var item in g.vertices)
            {
                Console.WriteLine("ID:"+item.id);
                Console.WriteLine("TableIRI:" + item.table.IRI);
                Console.WriteLine("OutP:" + item.outEdgePosition);
                Console.WriteLine("InP:" + item.inEdgePosition);
                Console.WriteLine("P:" +item.GetPositionInVertices());
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine("OutEdges");
            //displey whats inside edges
            foreach (var item in g.outEdges)
            {
                Console.WriteLine("ID:" + item.id);
                Console.WriteLine("TableIRI:" + item.table.IRI);
                Console.WriteLine("EndVertexID:" + item.endVertex.id);
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine("InEdges");
            foreach (var item in g.inEdges)
            {
                Console.WriteLine("ID:" + item.id);
                Console.WriteLine("TableIRI:" + item.table.IRI);
                Console.WriteLine("EndVertexID:" + item.endVertex.id);
                Console.WriteLine();
            }
            Console.ReadLine();
            */
            #endregion PRINT
        }


    }
}
