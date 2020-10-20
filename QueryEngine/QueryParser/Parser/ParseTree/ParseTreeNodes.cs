/*! \file
  
This file includes definitions of nodes used to create a parse tree of the query.
Nodes are used by a Parser to create the parse tree.

Each query clause has its root node. Such as select, match...
Grammars are defined in Parser files.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine { 
   
    /// <summary>
    /// Parent to every parse tree node.
    /// Gives Visit method.
    /// </summary>
    internal abstract class Node
    {
        public abstract void Accept<T>(IVisitor<T> visitor);
    }

    /// <summary>
    /// Certain nodes can form a chain. E.g variable node or vertex/edge node.
    /// Gives property next.
    /// </summary>
    internal abstract class NodeChain : Node
    {
        public Node next;
        public void AddNext(Node next)
        {
            this.next = next;
        }
    }

    #region RootNodes

    /// <summary>
    /// Match and Select Nodes are only roots of subtrees when parsing. From them the parsing
    /// of query word starts.
    /// </summary>
    internal class MatchNode : NodeChain
    {
        public MatchNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    internal class SelectNode : NodeChain
    {
        public SelectNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    internal class OrderByNode : NodeChain
    {
        public OrderByNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class GroupByNode : NodeChain
    {
        public GroupByNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    #endregion RootNodes

    #region SelectNodes

    internal class SelectPrintTermNode : NodeChain
    {
        public Node exp;

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }

        public void AddExpression(Node expr)
        {
            this.exp = expr;
        }
    }

    #endregion SelectNodes

    #region MatchNodes
    /// <summary>
    /// Only vertices and edges inherit from this class.
    /// Gives varible node property to the edges and vertices.
    /// </summary>
    internal abstract class CommomMatchNode : NodeChain
    {
        public Node matchVariable;

        public void AddMatchVariable(Node v)
        {
            this.matchVariable = v;
        }
    }

    internal abstract class EdgeNode : CommomMatchNode { }

    /// <summary>
    /// Edge node and Vertex node represents vertex and edge in the parsing tree. 
    /// They hold next property that leads to a next vertex/edge or match divider.
    /// </summary>

    internal class InEdgeNode : EdgeNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class OutEdgeNode : EdgeNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class AnyEdgeNode : EdgeNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class VertexNode : CommomMatchNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Match divider serves as a separator of multiple patterns in query.
    /// </summary>
    internal class MatchDividerNode : NodeChain
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class MatchVariableNode : Node
    {
        public Node variableName;
        public Node variableType;

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }

        public void AddVariableName(Node node)
        {
            this.variableName = node;
        }

        public void AddVariableType(Node node)
        {
            this.variableType = node;
        }

        public bool IsEmpty()
        {
            return (this.variableName == null && this.variableType == null);
        }

    }


    #endregion MatchNodes

    #region GroupByNodes

    internal class GroupByTermNode : NodeChain
    {
        public Node exp;
        public GroupByTermNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    #endregion GroupByNodes


    #region OrderByNodes

    /// <summary>
    /// Node representing one ordering.
    /// Contains information whether it is ascending order or descending and 
    /// expression to evaluate against.
    /// </summary>
    internal class OrderTermNode : NodeChain
    {
        public bool isAscending;
        public Node exp;
        public OrderTermNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }

        public void AddExpression(Node expr)
        {
            this.exp = expr;
        }

        public void SetIsAscending(bool isAscending)
        {
            this.isAscending = isAscending;
        }
    }




    #endregion OrderByNodes

    #region ExprNodes

    internal class ExpressionNode : Node
    {

        public Node exp;
        public Node asLabel;

        public override void Accept<T>(IVisitor<T> visitor)
        { 
            visitor.Visit(this);
        }

        public void AddLabel(Node label)
        {
            this.asLabel = label;
        }

        public void AddExpression(Node expr)
        {
            this.exp = expr;
        }

    }

    /// <summary>
    /// Varible node serves as a holder for Name of varibles and possibly selection of their properties.
    /// Identifier node hold the real value of variable.
    /// </summary>
    internal class VariableNode : Node
    {
        public Node name;
        public Node propName;
        public void AddName(Node n)
        {
            this.name = n;
        }

        public void AddProperty(Node p)
        {
            this.propName = p;
        }

        public bool IsEmpty()
        {
            if ((name == null) && (propName == null))
            {
                return true;
            }
            else return false;
        }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Stores a indetifier as a string.
    /// </summary>
    internal class IdentifierNode : Node
    {
        public string value { get; private set; }

        public IdentifierNode(string v) { this.value = v; }

        public void AddValue(string v) { this.value = v; }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    #endregion ExprNodes


    // Provisional count
    internal class CountProvisional : Node
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            throw new NotImplementedException();
        }
    }

}
