using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Simple test class for dumping and facading query requests.
    /// Not ment as a unit test class.
    /// </summary>
    class TestClass
    {
        public static void DumpGraph(Graph g)
        {

            /////////////
            Console.WriteLine();
            //Display whats inside dictionary of nodes 

            foreach (var item in g.NodeTables)
            {
                Console.WriteLine("Key:" + item.Key);
                Console.WriteLine("TableIri:" + item.Value.IRI);
                foreach (var ite in item.Value.Properties)
                {
                    Console.WriteLine("PropertyIRI:" + ite.IRI);
                    Console.WriteLine("PropertyType:" + ite.GetType());
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
                foreach (var ite in item.Value.Properties)
                {
                    Console.WriteLine("PropertyIRI:" + ite.IRI);
                    Console.WriteLine("PropertyType:" + ite.GetType());
                }

                Console.WriteLine();
            }
            Console.WriteLine();


            /////////////
            Console.WriteLine("Vertices");
            //Display whats inside vertices

            foreach (var item in g.vertices)
            {
                Console.WriteLine("ID:" + item.ID);
                Console.WriteLine("TableIRI:" + item.Table.IRI);
                Console.WriteLine("OutSP:" + item.OutEdgesStartPosition);
                Console.WriteLine("OutEP:" + item.OutEdgesEndPosition);

                Console.WriteLine("InSP:" + item.InEdgesStartPosition);
                Console.WriteLine("INEP:" + item.InEdgesEndPosition);
                Console.WriteLine("P:" + item.PositionInList);
                Console.WriteLine();
            }




            //////////////
            Console.WriteLine();
            Console.WriteLine("OutEdges");
            //displey whats inside edges


            foreach (var item in g.outEdges)
            {
                Console.WriteLine("ID:" + item.ID);
                Console.WriteLine("TableIRI:" + item.Table.IRI);
                Console.WriteLine("EndVertexID:" + item.EndVertex.ID);
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine("InEdges");
            foreach (var item in g.inEdges)
            {
                Console.WriteLine("ID:" + item.ID);
                Console.WriteLine("TableIRI:" + item.Table.IRI);
                Console.WriteLine("EndVertexID:" + item.EndVertex.ID);
                Console.WriteLine();
            }


        }

        public static void Search(Graph g)
        {


            List<Token> tokens = Tokenizer.Tokenize(Console.In);

            foreach (var item in tokens)
            {
                Console.WriteLine(item.type);
                if (item.type == Token.TokenType.Identifier) Console.WriteLine(item.strValue);
            }


            Parser.ResetPosition();
            var map = new VariableMap();

            SelectNode selectNode = Parser.ParseSelect(tokens);
            var match = new MatchObject(tokens, map, g, 2, 2);
            var select = new SelectObject(g, map, selectNode, "console", "simple");
            var order = OrderByObject.CreateOrderBy(tokens, g, map);

            var tmp = match.Search();


            Console.WriteLine("Results Ids");

            foreach (var item in tmp)
            {
                Console.WriteLine(item.ToString());
            }

            var tmp2 = order.Sort(tmp);

            select.Print(tmp2);



            Console.ReadLine();

        }

        public static void RunTest()
        {


 
           Graph g = new Graph();

            //just for testing
            ///////////////////////////////////////

            TestClass.DumpGraph(g);
            TestClass.Search(g);





        }


    }
}
