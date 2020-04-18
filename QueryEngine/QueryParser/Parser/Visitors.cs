
/*! \file
  This file includes definitions of visitor used to collect data from created parsed trees. 
  Each query object has its own visitor.
  Visitor iterates over nodes defined in ParseTreeNodes.cs
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Parse tree is processed via visitors
    /// </summary>
    /// <typeparam name="T"> Object built after parsing </typeparam>
    interface IVisitor<T>
    {
        T GetResult();
        void Visit(SelectNode node);
        void Visit(MatchNode node);
        void Visit(MatchDivider node);
        void Visit(VertexNode node);
        void Visit(EdgeNode node);
        void Visit(VariableNode node);
        void Visit(IdentifierNode node);
        void Visit(ExpressionNode node);
        void Visit(MatchVariableNode node);
    }


    /// <summary>
    /// Creates list of variable (Name.Prop) to be displayed in Select expr.
    /// </summary>
    class SelectVisitor : IVisitor<List<ExpressionHolder>>
    {
        List<ExpressionHolder> result;
        Dictionary<string, Type> Labels;
        VariableMap Map;

        public SelectVisitor(Dictionary<string, Type> labels, VariableMap map)
        {
            this.result = new List<ExpressionHolder>();
            this.Labels = labels;
            this.Map = map;
        }

        public List<ExpressionHolder> GetResult()
        {
            if (this.result == null || this.result.Count == 0) 
                throw new ArgumentException($"{this.GetType()} final result is empty or null");
             return this.result; 
        }


        /// <summary>
        /// Starts parsing from select node, does nothing only jumps to next node.
        /// There must be at least one variable to be displyed.
        /// </summary>
        /// <param name="node"> Select node </param>
        public void Visit(SelectNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException($"{ this.GetType()}, failed to parse select expr.");
        }



        /// <summary>
        /// Parses expression node. Expects variable.name as label
        /// </summary>
        /// <param name="node"></param>
        public void Visit(ExpressionNode node )
        {
            // parse the variable 

            // parse label and parse next 
            
            
            if (node.next == null) return;
            else node.next.Accept(this);

        }

        /// <summary>
        /// Create new variable and try parse its name and propname.
        /// Name shall never be null. Name is identifier node.
        /// Jump to next variable node.
        /// </summary>
        /// <param name="node"> Variable node </param>
        public void Visit(VariableNode node)
        {
            result.Add(new SelectVariable());
            if (node.name == null)
                throw new ArgumentException($"{this.GetType()}, could not parse variable name.");
            else
            {
                // Jump to identifier node with string value of name 
                node.name.Accept(this);
                // If the propname is set, jump to identifier node of property
                if (node.propName != null)
                {
                    node.propName.Accept(this);
                }
            }
        }

        /// <summary>
        /// Obtains string value of variable or property or label.
        /// </summary>
        /// <param name="node"> Identifier node </param>
        public void Visit(IdentifierNode node)
        {
            if (result[result.Count - 1].TrySetName(node.value)) return;
            else if (result[result.Count - 1].TrySetPropName(node.value)) return;
            else if (result[result.Count - 1].TrySetLabel(node.value)) return;
            else throw new ArgumentException($"{this.GetType()}, could not parse name.");
        }



        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(MatchDivider node)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(EdgeNode node)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(MatchVariableNode node)
        {
            throw new NotImplementedException();
        }


    }



    /// <summary>
    /// Creates List of single pattern chains which will form the whole pattern later in MatchQueryObject.
    /// </summary>
    class MatchVisitor : IVisitor<List<ParsedPattern>>
    {
        List<ParsedPattern> result;
        ParsedPattern currentPattern;
        Dictionary<string, Table> vTables;
        Dictionary<string, Table> eTables;
        bool readingName;
        bool readingVertex;

        public MatchVisitor(Dictionary<string, Table> v, Dictionary<string, Table> e)
        {
            this.currentPattern = new ParsedPattern();
            this.result = new List<ParsedPattern>();
            this.vTables = v;
            this.eTables = e;
            this.readingName = true;
            this.readingVertex = true;
        }


        public List<ParsedPattern> GetResult()
        { return this.result; }

        /// <summary>
        /// Jumps to vertex node.
        /// All patterns must have at least one match.
        /// There is always at least one ParsedPattern.
        /// </summary>
        /// <param name="node"> Match node </param>
        public void Visit(MatchNode node)
        {
            //Create new pattern and start its parsing.
            result.Add(currentPattern);
            node.next.Accept(this);

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].GetCount() <= 0)
                    throw new ArgumentException($"{this.GetType()}, failed to parse match expr.");
            }
        }



        /// <summary>
        /// Processes vertex node, try to jump to variable inside vertex or continue to the edge.
        /// </summary>
        /// <param name="node"> Vertex Node </param>
        public void Visit(VertexNode node)
        {
            this.readingVertex = true;

            ParsedPatternNode vm = new ParsedPatternNode();
            vm.isVertex = true;
            currentPattern.AddParsedPatternNode(vm);

            if (node.matchVariable != null) node.matchVariable.Accept(this);
            if (node.next != null) node.next.Accept(this);
        }

        /// <summary>
        /// Processes Edge node, tries to jump to variable node inside edge or to the next vertex.
        /// </summary>
        /// <param name="node"> Edge node </param>
        public void Visit(EdgeNode node)
        {
            this.readingVertex = false;

            ParsedPatternNode em = new ParsedPatternNode();
            em.edgeType = node.GetEdgeType();
            em.isVertex = false;
            currentPattern.AddParsedPatternNode(em);

            if (node.matchVariable != null) node.matchVariable.Accept(this);
            if (node.next == null)
                throw new ArgumentException($"{this.GetType()}, missing end vertex from edge.");
            else node.next.Accept(this);

        }


        /// <summary>
        /// Processes match variable node.
        /// Always jumps to identifier node where Name and type is processed.
        /// </summary>
        /// <param name="node"> Variable node </param>
        public void Visit(MatchVariableNode node)
        {
            readingName = true;
            //It is not anonnymous field.
            if (node.variableName != null)
            {
                node.variableName.Accept(this);
            }
            //It has set type.
            if (node.variableType != null)
            {
                readingName = false;
                node.variableType.Accept(this);
            }
        }

        /// <summary>
        /// Processes Identifier node.
        /// Either assigns name of variable to last ParsedPatternNode or table pertaining to the node. 
        /// </summary>
        /// <param name="node">Identifier node </param>
        public void Visit(IdentifierNode node)
        {
            ParsedPatternNode n = currentPattern.GetLastParsedPatternNode();

            if (readingName)
            {
                n.isAnonymous = false;
                n.name = node.value;
            }
            else
            {
                if (readingVertex) ProcessType(node, vTables, n);
                else ProcessType(node, eTables, n);
            }
        }

        /// <summary>
        /// Tries to find table based on indentifier node value and assign it to parsed node.
        /// </summary>
        /// <param name="node"> Identifier node from Visiting indetifier node.</param>
        /// <param name="d"> Dictionary of tables from edges/vertices. </param>
        /// <param name="n"> ParsedPatternNode from within Visiting identifier node.</param>
        private void ProcessType(IdentifierNode node, Dictionary<string, Table> d, ParsedPatternNode n)
        {
            //Try find the table of the variable, it has to be always valid table name.
            if (!d.TryGetValue(node.value, out Table table))
                throw new ArgumentException($"{this.GetType()}, could not parse Table name.");
            else n.table = table;
        }


        /// <summary>
        /// Serves as a dividor of multiple patterns.
        /// Create new pattern and start its parsing.
        /// </summary>
        /// <param name="node"> Match Divider node </param>
        public void Visit(MatchDivider node)
        {
            currentPattern = new ParsedPattern();
            result.Add(currentPattern);
            node.next.Accept(this);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>.
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(ExpressionNode node )
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(VariableNode node)
        {
            throw new NotImplementedException();
        }
        
    }
}
