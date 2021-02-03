/*! \file
  This file includes definitions of match visitor used to collect data from created parsed tree.
  It implements visits to a classes used inside a match parsed tree.
  Visitor creates a list of Parsed Patterns that are later used to creat a pattern used during matching algorithm.
*/

using System;
using System.Collections.Generic;

namespace QueryEngine
{
    internal sealed class MatchVisitor : IVisitor<List<ParsedPattern>>
    {
        private List<ParsedPattern> result;
        private ParsedPattern currentPattern;
        private Dictionary<string, Table> vTables;
        private Dictionary<string, Table> eTables;
        private bool readingName;
        private bool readingVertex;

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
        /// A root node of the parse tree.
        /// Jumps to the node under the root.
        /// All patterns must have at least one match.
        /// There is always at least one ParsedPattern.
        /// </summary>
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
        /// Try to jump to variable inside vertex or continue to the edge.
        /// </summary>
        public void Visit(VertexNode node)
        {
            this.readingVertex = true;

            ParsedPatternNode vm = new VertexParsedPatternNode();
            currentPattern.AddParsedPatternNode(vm);

            if (node.matchVariable != null) node.matchVariable.Accept(this);
            if (node.next != null) node.next.Accept(this);
        }

        /// <summary>
        /// Tries to jump to variable node inside edge or to the next vertex.
        /// </summary>
        public void Visit(InEdgeNode node)
        {
            this.readingVertex = false;

            ParsedPatternNode em = new InEdgeParsedPatternNode();
            currentPattern.AddParsedPatternNode(em);

            if (node.matchVariable != null) node.matchVariable.Accept(this);
            if (node.next == null)
                throw new ArgumentException($"{this.GetType()}, missing end vertex from edge.");
            else node.next.Accept(this);
        }

        /// <summary>
        /// Tries to jump to variable node inside edge or to the next vertex.
        /// </summary>
        public void Visit(OutEdgeNode node)
        {
            this.readingVertex = false;

            ParsedPatternNode em = new OutEdgeParsedPatternNode();
            currentPattern.AddParsedPatternNode(em);

            if (node.matchVariable != null) node.matchVariable.Accept(this);
            if (node.next == null)
                throw new ArgumentException($"{this.GetType()}, missing end vertex from edge.");
            else node.next.Accept(this);

        }

        /// <summary>
        /// Tries to jump to variable node inside edge or to the next vertex.
        /// </summary>
        public void Visit(AnyEdgeNode node)
        {
            this.readingVertex = false;

            ParsedPatternNode em = new AnyEdgeParsedPatternNode();
            currentPattern.AddParsedPatternNode(em);

            if (node.matchVariable != null) node.matchVariable.Accept(this);
            if (node.next == null)
                throw new ArgumentException($"{this.GetType()}, missing end vertex from edge.");
            else node.next.Accept(this);
        }
        
        /// <summary>
        /// Always jumps to identifier node where Name and type is processed.
        /// </summary>
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
        /// Either assigns name of a variable to the last ParsedPatternNode or there is a table pertaining to the node. 
        /// It returns from here because there is no other node to visit.
        /// </summary>
        public void Visit(IdentifierNode node)
        {
            ParsedPatternNode n = currentPattern.GetLastParsedPatternNode();

            if (readingName)
            {
                n.IsAnonymous = false;
                n.Name = node.value;
            }
            else
            {
                if (readingVertex) ProcessType(node, vTables, n);
                else ProcessType(node, eTables, n);
            }
        }

        /// <summary>
        /// Tries to find a table based on the indentifier node value and assign it to the parsed node.
        /// </summary>
        /// <param name="node"> Identifier node from Visiting indetifier node.</param>
        /// <param name="d"> Dictionary of tables from edges/vertices. </param>
        /// <param name="n"> ParsedPatternNode from within Visiting identifier node.</param>
        private void ProcessType(IdentifierNode node, Dictionary<string, Table> d, ParsedPatternNode n)
        {
            //Try find the table of the variable, it has to be always valid table name.
            if (!d.TryGetValue(node.value, out Table table))
                throw new ArgumentException($"{this.GetType()}, could not parse Table name.");
            else n.Table = table;
        }


        /// <summary>
        /// Serves as a dividor of multiple patterns.
        /// Create new pattern and start its parsing.
        /// </summary>
        public void Visit(MatchDividerNode node)
        {
            currentPattern = new ParsedPattern();
            result.Add(currentPattern);
            node.next.Accept(this);
        }

        #region NotImpl

        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ExpressionNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(OrderByNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(OrderTermNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(SelectPrintTermNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(GroupByNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(GroupByTermNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AggregateFuncNode node)
        {
            throw new NotImplementedException();
        }
        #endregion NotImpl
    }

}
