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
        public List<ScopeVariable> scopeVariables;



    }

    class ScopeVariable
    {
        public string name;
        public int positionInPattern;

        public void AddVariableName(string n) => this.name = n;
        public void AddPositionInPattern(int p) => this.positionInPattern = p;
    }



    //Select represents list of variables to print.
   class SelectObject
   {
        public List<SelectVariable> selectVariables;
        public SelectObject(SelectNode node)
        {
        }

   }

    class SelectVariable
    {
        public string name { get; private set; } 
        public string propName { get; private set; }

        public bool TrySetName(string n)
        {
            if (this.name == null) { this.name = n; return true; }
            else return false;
        }
        public bool TrySetPropName(string n)
        {
            if (this.propName == null) { this.propName = n; return true; }
            else return false;
        }
        
    }




    //Match represents patter to match in main match algorithm.
   class MatchObject
   {
        List<BaseMatch> pattern;


        public MatchObject(MatchNode node)
        {
        }


   }

    abstract class BaseMatch
    {
        public abstract bool Apply(Field element);
    }

    class VertexMatch : BaseMatch
    {
        public override bool Apply(Field element)
        {
            throw new NotImplementedException();
        }
    }

    class EdgeMatch : BaseMatch
    {
        public override bool Apply(Field element)
        {
            throw new NotImplementedException();
        }
    }





}
