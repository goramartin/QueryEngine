using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    class Query
    {
        SelectObject selectObject;
        MatchObject matchObject;
        Scope scope;


        public Query(SelectNode s, MatchNode m)
        {
            selectObject = new SelectObject(s);
            matchObject = new MatchObject(m);

        }
    }

    class Scope
    {




    }

   class SelectObject
   {

        public SelectObject(SelectNode node)
        {

        }



   }

   class MatchObject
   {


        public MatchObject(MatchNode node)
        {



        }

    
   }
}
