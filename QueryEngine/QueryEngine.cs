﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace QueryEngine
{
    class QueryEngine
    {
        
        private static Query CreateQuery(TextReader reader) 
        {
            List<Token> tokens = Tokenizer.Tokenize(reader);

            Parser.ResetPosition();
            SelectNode selectNode = Parser.ParseSelectExpr(tokens);
            MatchNode matchNode = Parser.ParseMatchExpr(tokens);

            //to do better if it returns null;
            if (tokens.Count != Parser.GetPosition()) 
                throw new ArgumentException("Failed to parse every token."); 

            Query query = new Query(selectNode, matchNode);

            return query;
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
            Graph g = CreateGraph(args);

            Query query = CreateQuery(reader);    








        }

        static void Main(string[] args)
        {
            try
            {
                Run(args, Console.In, Console.Out);
                return;
            }
            catch (Exception e )
            {
                Console.WriteLine( e.Message);
            }
           




            List<Token> tokens = Tokenizer.Tokenize(Console.In);

            foreach (var item in tokens)
            {
                Console.WriteLine(item.type);
                if (item.type == Token.TokenType.Identifier) Console.WriteLine(item.strValue) ;
            }

            Parser p = new Parser();
            SelectNode d = (SelectNode)p.ParseSelectExpr(tokens);
            MatchNode s =(MatchNode) p.ParseMatchExpr(tokens);

            Console.ReadLine();

            /*
            Graph g = new Graph();

            /////////////
            g.LoadNodeTables("VertexTypes.txt");
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
            g.LoadEdgeTables("EdgeTypes.txt");
            
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
            g.LoadEdgeList("NodesEdges.txt");
            
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
            */
        }
    }
}