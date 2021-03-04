using System;

namespace QueryEngine
{
    /// <summary>
    /// Simple test class for dumping and facading query requests.
    /// Not ment as a unit test class.
    /// </summary>
    class DumpGraphClass
    {
        public static void DumpGraph(Graph g)
        {
            /////////////
            Console.WriteLine();
            //Display whats inside dictionary of nodes 

            foreach (var item in g.nodeTables)
            {
                Console.WriteLine(
                    " Key:" + item.Key + 
                    " TableIri:" + item.Value.IRI
                    );
                foreach (var ite in item.Value.Properties)
                {
                    Console.Write(
                        " PropertyIRI:" + ite.Value.IRI +
                        " PropertyType:" + ite.Value.GetType() + " / "
                        );
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            /////////////
            //Display whats inside dictionary of edges

            foreach (var item in g.edgeTables)
            {
                Console.WriteLine(
                    " Key:" + item.Key + 
                    " TableIri:" + item.Value.IRI
                    );
                
                foreach (var ite in item.Value.Properties)
                {
                    Console.Write(
                        " PropertyIRI:" + ite.Value.IRI + 
                        " PropertyType:" + ite.Value.GetType() + " / "
                        );
                }

                Console.WriteLine();
            }
            Console.WriteLine();


            /////////////
            Console.WriteLine("Vertices");
            //Display whats inside vertices

            foreach (var item in g.vertices)
            {
                Console.WriteLine(
                    " ID:" + item.ID + 
                    " TableIRI:" + item.Table.IRI +
                    " OutSP:" + item.OutEdgesStartPosition +
                    " OutEP:" + item.OutEdgesEndPosition + 
                    " InSP:" + item.InEdgesStartPosition +
                    " INEP:" + item.InEdgesEndPosition +
                    " P:" + item.PositionInList
                    );
                Console.WriteLine();
            }
            Console.WriteLine();

            //////////////
            Console.WriteLine("OutEdges");
            //displey whats inside edges

            foreach (var item in g.outEdges)
            {
                Console.WriteLine(
                " ID:" + item.ID +
                " TableIRI:" + item.Table.IRI +
                " EndVertexID:" + item.EndVertex.ID
                );
                Console.WriteLine();
            }
            Console.WriteLine();

            Console.WriteLine("InEdges");
            foreach (var item in g.inEdges)
            {
                Console.WriteLine(
                " ID:" + item.ID +
                " TableIRI:" + item.Table.IRI +
                " EndVertexID:" + item.EndVertex.ID
                );
                Console.WriteLine();
            }
        }

        public static void Dump()
        {
           Graph g = new Graph();
           DumpGraphClass.DumpGraph(g);
        }
    }
}
