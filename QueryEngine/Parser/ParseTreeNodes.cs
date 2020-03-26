
/*
 * This file includes definitions of nodes used to create a parse tree of the query.
 * Nodes are used by a Parser to create the parse tree.
 */




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine { 
    /// <summary>
    ///Parent to every node.
    ///Gives Visit method.
    /// </summary>
    abstract class Node
    {
        public abstract void Accept<T>(IVisitor<T> visitor);
    }

    /// <summary>
    /// Certain nodes can form a chain. E.g variable node or vertex/edge node.
    /// Gives property next.
    /// </summary>
    abstract class QueryNode : Node
    {
        public Node next;
        public void AddNext(Node next)
        {
            this.next = next;
        }
    }

    /// <summary>
    /// Only vertices and edges inherit from this class.
    /// Gives varible node property to the edges and vertices.
    /// </summary>
    abstract class CommomMatchNode : QueryNode
    {
        public Node variable;

        public void AddVariable(Node v)
        {
            this.variable = v;
        }
    }

    /// <summary>
    /// Match and Select Nodes are only roots of subtrees when parsing. From them the parsing
    /// of query word starts.
    /// </summary>
    class MatchNode : QueryNode
    {
        public MatchNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class SelectNode : QueryNode
    {
        public SelectNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }


    /// <summary>
    /// Edge node and Vertex node represents vertex and edge in the parsing tree. 
    /// They hold next property that leads to a next vertex/edge or match divider.
    /// Match divider servers as a separator of multiple patterns in query.
    /// </summary>

    class EdgeNode : CommomMatchNode
    {
        EdgeType type;
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
        public void SetEdgeType(EdgeType type)
        {
            this.type = type;
        }
        public EdgeType GetEdgeType()
        {
            return this.type;
        }

    }
    class VertexNode : CommomMatchNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class MatchDivider : QueryNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Varible node serves as a holder for Name of varibles and possibly selection of their properties.
    /// Identifier node hold the real value of variable.
    /// </summary>
    class VariableNode : QueryNode
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
}
