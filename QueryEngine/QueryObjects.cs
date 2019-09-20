using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    //Query represents query information, carrying it in each of the query main words.
    class Query
    {
        SelectObject selectObject;
        MatchObject matchObject;
        Scope scope;

        public Query(SelectNode s, MatchNode m)
        {
            selectObject = new SelectObject(s);
            matchObject = new MatchObject(m);
            CreateScope();
        }

        private bool CreateScope()
        {
            this.scope = new Scope();
            return true;

        }
    }

    //Scope represents scope of variable in the whole query.
    class Scope
    {




    }

    //Select represents list of variables to print.
   class SelectObject
   {

        public SelectObject(SelectNode node)
        {
        }

   }

    //Match represents patter to match in main match algorithm.
   class MatchObject
   {
        public MatchObject(MatchNode node)
        {
        }
   }
}
