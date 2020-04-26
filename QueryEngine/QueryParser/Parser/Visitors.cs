
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
        void Visit(MatchDividerNode node);
        void Visit(VertexNode node);
        void Visit(EdgeNode node);
        void Visit(VariableNode node);
        void Visit(IdentifierNode node);
        void Visit(ExpressionNode node);
        void Visit(MatchVariableNode node);
        void Visit(OrderByNode node);
        void Visit(OrderTermNode node);
        void Visit(SelectPrintTermNode node);
    }

    /// <summary>
    /// Creates list of variable (Name.Prop) to be displayed in Select expr.
    /// </summary>
    class SelectVisitor : IVisitor<List<ExpressionHolder>>
    {
        List<ExpressionHolder> result;
        Dictionary<string, Type> Labels;
        VariableMap variableMap;

        public SelectVisitor(Dictionary<string, Type> labels, VariableMap map)
        {
            this.result = new List<ExpressionHolder>();
            this.Labels = labels;
            this.variableMap = map;
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
        public void Visit(ExpressionNode node )
        {
            string label = null;
            ExpressionBase expr = null;

            // Parse expression.
            if (node.exp == null) 
                throw new ArgumentException($"{this.GetType()}, Expected expression.");
            else
            {
                var tmpVisitor = new ExpressionNodesVisitor(this.variableMap, this.Labels);
                node.exp.Accept(tmpVisitor);
                expr = tmpVisitor.GetResult();
            }

            // Try get a label for entire expression.
            if (node.asLabel != null)
                label = ((IdentifierNode)(node.asLabel)).value;

            this.result.Add(new ExpressionHolder(expr, label));

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
                this.result.Add( new ExpressionHolder(new VariableIDReference(new VariableReferenceNameHolder(item.Key), item.Value.Item1),null));

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
    
        public void Visit(ExpressionNode node )
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


    /// <summary>
    /// Creates a list or comparers that will be used during ordering of match results.
    /// </summary>
    class OrderByVisitor : IVisitor<List<IRowProxyComparer>>
    {
        List<IRowProxyComparer> result;
        Dictionary<string, Type> Labels;
        VariableMap variableMap;
        ExpressionHolder expressionHolder;

        public OrderByVisitor(Dictionary<string, Type> labels, VariableMap map)
        {
            this.result = new List<IRowProxyComparer>();
            this.Labels = labels;
            this.variableMap = map;
        }

        public List<IRowProxyComparer> GetResult()
        {
            if (this.result == null || this.result.Count == 0)
                throw new ArgumentException($"{this.GetType()} final result is empty or null");
            return this.result;
        }

        public void Visit(OrderByNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException($"{ this.GetType()}, failed to parse select expr.");
        }

        public void Visit(OrderTermNode node)
        {
            if (node.exp == null) throw new ArgumentNullException($"{this.GetType()}, failed access expression.");
            else node.exp.Accept(this);

            this.result.Add(ExpressionComparer.
                            ExpressionCompaperFactory(this.expressionHolder, node.isAscending,
                                                      this.expressionHolder.GetExpressionType()));

            if (node.next != null) node.next.Accept(this);
        }
        public void Visit(ExpressionNode node)
        {
            string label = null;
            ExpressionBase expr = null;

            // Parse expression.
            if (node.exp == null)
                throw new ArgumentException($"{this.GetType()}, expected expression.");
            else
            {
                var tmpVisitor = new ExpressionNodesVisitor(this.variableMap, this.Labels);
                node.exp.Accept(tmpVisitor);
                expr = tmpVisitor.GetResult();
            }

            // Try get a label for entire expression.
            if (node.asLabel != null)
                label = ((IdentifierNode)(node.asLabel)).value;

            this.expressionHolder = new ExpressionHolder(expr, label);
        }

        #region NotImpl
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchDividerNode node)
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

        public void Visit(VariableNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MatchVariableNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(SelectPrintTermNode node)
        {
            throw new NotImplementedException();
        }

        #endregion NotImpl

    }


    /// <summary>
    /// Visitor used to parse expressions.
    /// So far there are implemented only variable references as a expression.
    /// </summary>
    class ExpressionNodesVisitor : IVisitor<ExpressionBase>
    {
        ExpressionBase Expr;
        VariableReferenceNameHolder nameHolder;
        VariableMap variableMap;
        Dictionary<string, Type> Labels;

        public ExpressionBase GetResult()
        {
            return this.Expr;
        }

        public ExpressionNodesVisitor(VariableMap map, Dictionary<string, Type> labels)
        {
            this.variableMap = map;
            this.Labels = labels;
        }

        /// <summary>
        /// Visits variable node. 
        /// If it consists only of a name, variable id reference is created.
        /// Otherwise propperty reference will be created.
        /// </summary>
        /// <param name="node"> Variable node.</param>
        public void Visit(VariableNode node)
        {
            this.nameHolder = new VariableReferenceNameHolder();

            if (node.name == null)
                throw new ArgumentException($"{this.GetType()}, expected name of a variable.");
            else
            {
                node.name.Accept(this);
                if (node.propName != null)
                    node.propName.Accept(this);
            }

            // Get the position of the variable in the result.
            int varIndex = this.variableMap.GetVariablePosition(this.nameHolder.Name);
            if (this.nameHolder.PropName == null)
                this.Expr = new VariableIDReference(this.nameHolder, varIndex);
            else
            {
                // Get type of accessed property.
                if (!this.Labels.TryGetValue(this.nameHolder.PropName, out Type propType))
                    throw new ArgumentException($"{this.GetType()}, property {this.nameHolder.PropName} does not exists in the graph.");
                else
                    this.Expr = VariableReferencePropertyFactory.Create(this.nameHolder, varIndex, propType);
            }

        }

        /// <summary>
        /// Visits identifier node.
        /// Sets only name of variable and name of accessed property.
        /// </summary>
        /// <param name="node">Identifier node. </param>
        public void Visit(IdentifierNode node)
        {
            if (node.value == null) throw new ArgumentNullException($"{this.GetType()}, identifier value is set to null.");
            else if (this.nameHolder.TrySetName(node.value)) return;
            else if (this.nameHolder.TrySetPropName(node.value)) return;
            else throw new ArgumentException($"{this.GetType()}, expected new name holder.");
        }

        #region NotImpl

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Visit(SelectNode node)
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
        public void Visit(MatchDividerNode node)
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
        public void Visit(ExpressionNode node)
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
