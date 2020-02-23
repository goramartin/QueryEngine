using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    class TestClass
    {
        public static void Run()
        {
            #region PRINT


 
           Graph g = new Graph();
           g.LoadNodeTables("VertexTypes.txt");
           g.LoadEdgeTables("EdgeTypes.txt");
            //g.LoadData("NodesEdges.txt");
            g.LoadVertices("Nodes.txt");
            g.LoadEdges("Edges.txt");


           //just for testing
           ///////////////////////////////////////
           /*
           Console.ReadLine();

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
           Console.WriteLine(q.CheckCorrectnessOfQuery());



           Console.WriteLine();
           DFSPatternMatcher dfs = new DFSPatternMatcher(l, g);
           dfs.Search();



           Console.ReadLine();

           */



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
           

           /////////////
           Console.WriteLine();
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


           /////////////
           Console.WriteLine("Vertices");
           //Display whats inside vertices

           foreach (var item in g.vertices)
           {
               Console.WriteLine("ID:"+item.id);
               Console.WriteLine("TableIRI:" + item.table.IRI);
               Console.WriteLine("OutSP:" + item.outEdgesStartPosition);
                Console.WriteLine("OutEP:" + item.outEdgesEndPosition);

                Console.WriteLine("InSP:" + item.inEdgesStartPosition);
                Console.WriteLine("INEP:" + item.inEdgesEndPosition);
               Console.WriteLine("P:" +item.GetPositionInVertices());
               Console.WriteLine();
           }


           
            
            //////////////
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
           
            #endregion PRINT
            

        }


    }
}
