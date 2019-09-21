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

        public Query(SelectObject s, MatchObject m, Scope scope)
        {
            this.selectObject = s;
            this.matchObject = m;
            this.scope = scope;
        }

    }






    //Scope represents scope of variable in the whole query.
    class Scope
    {
        private Dictionary<string, int > scopeVar;

        public Scope(Dictionary<string,int> sv)
        {
            this.scopeVar = sv;
        }
        public Scope() 
        {
            scopeVar = new Dictionary<string, int>();
        }

        public Dictionary<string, int> GetScopeVariables() => this.scopeVar;
    }


    //Select represents list of variables to print.
   class SelectObject
   {
        private List<SelectVariable> selectVariables;
        public SelectObject(List<SelectVariable> l)
        {
            this.selectVariables = l;
        }

        public List<SelectVariable> GetSelectVariables() => this.selectVariables;
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
        public bool IsEmpty()
        {
            if ((this.name == null) && (this.propName == null)) return true;
            else return false;
        }
    }













    //Match represents patter to match in main match algorithm.
   class MatchObject
   {
        List<BaseMatch> pattern;

        public MatchObject(List<BaseMatch> p)
        {
            this.pattern = p;
        }

        public List<BaseMatch> GetPattern() => this.pattern;
   }

    abstract class BaseMatch
    {
        protected bool anonnymous;
        protected bool repeated;
        protected int positionOfRepeatedField;
        protected Table type;

        public abstract bool Apply(Field element);

        public void SetIsAnnonymous(bool b) => this.anonnymous = b;
        public void SetRepeated(bool b) => this.repeated = b;
        public void SetPositionOfRepeatedField(int p) => this.positionOfRepeatedField = p;
        public void SetType(Table t) => this.type = t;
        public Table GetTable() => this.type;

    }

    class VertexMatch : BaseMatch
    {
        public VertexMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeated = false;
            this.type = null;
        }

        public override bool Apply(Field element)
        {
            throw new NotImplementedException();
        }
    }
    class EdgeMatch : BaseMatch
    {
        protected EdgeType edgeType;

        public EdgeMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeated = false;
            this.type = null;
        }

        public override bool Apply(Field element)
        {
            throw new NotImplementedException();
        }

        public EdgeType GetEdgeType() => this.edgeType;
        public void SetEdgeType(EdgeType type) => this.edgeType = type;


    }





}
