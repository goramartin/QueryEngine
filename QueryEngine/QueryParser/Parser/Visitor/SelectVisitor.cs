﻿
/*! \file
  This file includes definitions of select visitor used to collect data from created parsed tree.
  It implements visits to a classes used inside a select parsed tree.
  Visitor creates a list of print variables that are used during printing query results.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Creates list of variable (Name.Prop) to be displayed in Select expr.
    /// </summary>
    sealed class SelectVisitor : IVisitor<List<PrintVariable>>
    {
        List<PrintVariable> result;
        Dictionary<string, Type> Labels;
        VariableMap variableMap;

        public SelectVisitor(Dictionary<string, Type> labels, VariableMap map)
        {
            this.result = new List<PrintVariable>();
            this.Labels = labels;
            this.variableMap = map;
        }

        public List<PrintVariable> GetResult()
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
        /// Parses print term node.
        /// Expects expression node and possibly next print term node.
        /// Together it creates a chain of print expressions.
        /// </summary>
        /// <param name="node">Select print term node. </param>
        public void Visit(SelectPrintTermNode node)
        {
            if (node.exp == null)
                throw new ArgumentNullException($"{this.GetType()}, failed to access expression.");
            else node.exp.Accept(this);

            if (node.next != null) node.next.Accept(this);
        }

        /// <summary>
        /// Parses expression node. Expects "variable.name as label"
        /// Parses expression nodes and tries to get a label for the expression.
        /// At the end it creates a expression holder.
        /// </summary>
        /// <param name="node"></param>
        public void Visit(ExpressionNode node)
        {
            string label = null;
            ExpressionBase expr = null;

            // Parse expression.
            if (node.exp == null)
                throw new ArgumentException($"{this.GetType()}, Expected expression.");
            else
            {
                var tmpVisitor = new ExpressionVisitor(this.variableMap, this.Labels);
                node.exp.Accept(tmpVisitor);
                expr = tmpVisitor.GetResult();
            }

            // Try get a label for entire expression.
            if (node.asLabel != null)
                label = ((IdentifierNode)(node.asLabel)).value;

            var tmpExprHolder = new ExpressionHolder(expr, label);
            this.result.Add(PrintVariable.PrintVariableFactory(tmpExprHolder, tmpExprHolder.ExpressionType));

        }

        /// <summary>
        /// Parses asterix. That means that there are as many expressions as variables.
        /// For each variable, expression that consists only of reference id will be created.
        /// </summary>
        public void Visit(VariableNode node)
        {
            if (node.name == null || ((IdentifierNode)node.name).value != "*")
                throw new ArgumentException($"{this.GetType()}, expected asterix.");

            foreach (var item in variableMap)
            {
                var tmpExprHolder = new ExpressionHolder(new VariableIDReference(new VariableReferenceNameHolder(item.Key), item.Value.Item1), null);
                this.result.Add(PrintVariable.PrintVariableFactory(tmpExprHolder, tmpExprHolder.ExpressionType));

            }

        }

        #region NotImpl

        public void Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchDividerNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(EdgeNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchVariableNode node)
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



        #endregion NotImpl

    }


}