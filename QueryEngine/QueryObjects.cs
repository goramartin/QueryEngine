﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    //Query represents query information, carrying it in each of the query main words.
    class Query
    {
        SelectObject select;
        MatchObject match;
        Scope scope;

        public Query(SelectObject s, MatchObject m, Scope scope)
        {
            this.select = s;
            this.match = m;
            this.scope = scope;
        }


        //Check if variables in select correspond to variables in scope
        public bool CheckCorrectnessOfQuery()
        {
            var sc = scope.GetScopeVariables();
            var pattern = match.GetPattern();
            foreach (var item in select.GetSelectVariables())
            {
                if (!sc.TryGetValue(item.name, out int p)) return false;
                
                //If select needs property, we check if the property is correct.
                if (item.propName != null) 
                {
                    if (pattern[p].GetTable() == null) return false;
                    if (!pattern[p].GetTable().ContainsProperty(item.propName)) return false;    
                }
            }
            return CheckCorrectnessOfPatternTypes(pattern);
        } 

        //Check whether in the pattern the repeating variables are same.
        private bool CheckCorrectnessOfPatternTypes(List<BaseMatch> pattern)
        {
            for (int i = 0; i < pattern.Count; i++)
            {
                BaseMatch b = pattern[i];
                if (b.IsRepeated())
                {
                    if (!b.Equals(b.GetPositionOfRepeatedField())) return false;
                }
            }
            return true;
        }

    }






    //Scope represents scope of variable in the whole query.
    class Scope
    {
        private Dictionary<string, int > scopeVar;
        public Scope(Dictionary<string,int> sv)=> this.scopeVar = sv;
        public Scope() => this.scopeVar = new Dictionary<string, int>();
        public Dictionary<string, int> GetScopeVariables() => this.scopeVar;
    }


    //Select represents list of variables to print.
   class SelectObject
   {
        private List<SelectVariable> selectVariables;
        public SelectObject(List<SelectVariable> sv) => this.selectVariables = sv;
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
        private List<BaseMatch> pattern;
        public MatchObject(List<BaseMatch> p) => this.pattern = p;
        public List<BaseMatch> GetPattern() => this.pattern;

   }

    //Class representing single step of pattern to match.
    //Method apply returns true if the element can be added to final result.
    abstract class BaseMatch
    {
        protected bool anonnymous;
        protected bool repeatedVariable;
        protected int positionOfRepeatedField;
        protected Table type;

        public abstract bool Apply(Element element, List<BaseMatch> baseMatches, Element[] result);

        protected bool CheckCommonConditions(Element element, List<BaseMatch> baseMatches, Element[] result)
        {
            //Check type, comparing references to tables.
            if ((this.type != null) && (this.type != element.GetTable())) return false;
            
            //It is anonnymous, then it can match any vertex/edge.
            if (this.anonnymous) return true;

            //It is repetition of variable before, check if it has same id.
            if (repeatedVariable)
            {
                if (result[positionOfRepeatedField].GetID() != element.GetID()) return false;
            }

            //Check if the element is not set for another variable.
            //Result length and baseMatches count are same.
            for (int i = 0; i < result.Length; i++)
            {
                Element tmpEl = result[i];
                
                //Further ahead, there are no elements stored in result.
                if (tmpEl == null) break;
                
                if (tmpEl.GetID() == element.GetID())
                {
                    if (baseMatches[i].IsAnonnymous()) continue;
                    else if (i == positionOfRepeatedField) continue;
                    else return false;
                }
            }

            return true;
        }


        public void SetAnnonymous(bool b) => this.anonnymous = b;
        public void SetRepeated(bool b) => this.repeatedVariable = b;
        public void SetPositionOfRepeatedField(int p) => this.positionOfRepeatedField = p;
        public void SetType(Table t) => this.type = t;
        public Table GetTable() => this.type;
        public bool IsRepeated() => this.repeatedVariable;
        public int GetPositionOfRepeatedField() => this.positionOfRepeatedField;
        public bool IsAnonnymous() => this.anonnymous;

    }

    class VertexMatch : BaseMatch
    {
        public VertexMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        public override bool Apply(Element element, List<BaseMatch> baseMatches, Element[] result)
        {
            if (!(element is Vertex)) return false;
            else return CheckCommonConditions(element, baseMatches, result);
        }


        public override bool Equals(object obj)
        {
            if (obj is VertexMatch)
            {
                var o = obj as VertexMatch;
                if (this.GetTable() != o.GetTable()) return false;
                return true;
            }
            return false;

        }

    }
    class EdgeMatch : BaseMatch
    {
        protected EdgeType edgeType;

        public EdgeMatch()
        {
            this.anonnymous = true;
            this.positionOfRepeatedField = -1;
            this.repeatedVariable = false;
            this.type = null;
        }

        public override bool Apply(Element element, List<BaseMatch> baseMatches, Element[] result)
        {
            if (!(element is Edge)) return false;
            else return CheckCommonConditions(element, baseMatches, result);
        }

        public EdgeType GetEdgeType() => this.edgeType;
        public void SetEdgeType(EdgeType type) => this.edgeType = type;

        public override bool Equals(object obj)
        {
            if (obj is EdgeMatch)
            {
                var o = obj as EdgeMatch;
                if (o.edgeType != this.edgeType) return false;
                if (this.GetTable() != o.GetTable()) return false;
                return true;
            }
            return false;

        }

    }





}
