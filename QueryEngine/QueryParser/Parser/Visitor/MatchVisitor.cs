﻿/*! \file
  This file includes definitions of match visitor used to collect data from created parsed tree.
  It implements visits to a classes used inside a match parsed tree.
  Visitor creates a list of Parsed Patterns that are later used to creat a pattern used during matching algorithm.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{


    /// <summary>
    /// Creates List of single pattern chains which will form the whole pattern later in MatchQueryObject.
    /// </summary>
    sealed class MatchVisitor : IVisitor<List<ParsedPattern>>
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
        #endregion NotImpl
    }

}
