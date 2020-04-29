
/*! \file
  This file includes definitions of nodes used to create a parse tree of the query.
  Nodes are used by a Parser to create the parse tree.
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
    abstract class Node
    {
        public abstract void Accept<T>(IVisitor<T> visitor);
    }

    /// <summary>
    /// Certain nodes can form a chain. E.g variable node or vertex/edge node.
    /// Gives property next.
    /// </summary>
    abstract class NodeChain : Node
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
    class MatchNode : NodeChain
    {
        public MatchNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class SelectNode : NodeChain
    {
        public SelectNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class OrderByNode : NodeChain
    {
        public OrderByNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    #endregion RootNodes

    #region SelectNodes

    class SelectPrintTermNode : NodeChain
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
    abstract class CommomMatchNode : NodeChain
    {
        public Node matchVariable;

        public void AddMatchVariable(Node v)
        {
            this.matchVariable = v;
        }
    }
    /// <summary>
    /// Edge node and Vertex node represents vertex and edge in the parsing tree. 
    /// They hold next property that leads to a next vertex/edge or match divider.
    /// </summary>

    class EdgeNode : CommomMatchNode
    {
        public EdgeType edgeType;
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
        public void SetEdgeType(EdgeType type)
        {
            this.edgeType = type;
        }
        public EdgeType GetEdgeType()
        {
            return this.edgeType;
        }

    }
    class VertexNode : CommomMatchNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Match divider serves as a separator of multiple patterns in query.
    /// </summary>
    class MatchDividerNode : NodeChain
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    class MatchVariableNode : Node
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

    #region OrderByNodes

    /// <summary>
    /// Node representing one ordering.
    /// Contains information whether it is ascending order or descending and 
    /// expression to evaluate against.
    /// </summary>
    class OrderTermNode : NodeChain
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

    class ExpressionNode : Node
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
    class VariableNode : Node
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
    class IdentifierNode : Node
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
}
