/*! \file
 
    This file include definitions of a specialised matches used to form a dfs pattern.
    There is a vertex and edge match class. The edge class further creates a children classes
    based on the edge orientation.
 
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Defines vertex match node.
    /// Description is provided inside abstract parent.
    /// </summary>
    sealed class DFSVertexMatch : DFSBaseMatch
    {
        public DFSVertexMatch() : base()
        { }

        public DFSVertexMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override bool Apply(Element element, Element[] map)
        {
            if (element == null) return false;
            else if (!(element is Vertex)) return false;
            else return CheckCommonConditions(element, map);
        }

    }

    /// <summary>
    /// Defines vertex match node.
    /// Description is provided inside abstract parent.
    /// </summary>
    abstract class DFSEdgeMatch : DFSBaseMatch
    {
        public DFSEdgeMatch() : base()
        { }

        public DFSEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public abstract EdgeType GetEdgeType();



    }

    /// <summary>
    /// Defines vertex match node.
    /// Description is provided inside abstract parent.
    /// </summary>
    sealed class DFSInEdgeMatch : DFSEdgeMatch
    {
        public DFSInEdgeMatch() : base()
        { }
        public DFSInEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.InEdge;

        public override bool Apply(Element element, Element[] map)
        {
            if (element == null) return false;
            else if (!(element is InEdge)) return false;
            else return CheckCommonConditions(element, map);
        }

    }

    /// <summary>
    /// Defines vertex match node.
    /// Description is provided inside abstract parent.
    /// </summary>
    sealed class DFSOutEdgeMatch : DFSEdgeMatch
    {
        public DFSOutEdgeMatch() : base()
        { }
        public DFSOutEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.OutEdge;

        public override bool Apply(Element element, Element[] map)
        {
            if (element == null) return false;
            else if (!(element is OutEdge)) return false;
            else return CheckCommonConditions(element, map);
        }
    }

    sealed class DFSAnyEdgeMatch : DFSEdgeMatch
    {
        public DFSAnyEdgeMatch() : base()
        { }
        public DFSAnyEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.AnyEdge;

        public override bool Apply(Element element, Element[] map)
        {
            if (element == null) return false;
            else if (!(element is Edge)) return false;
            else return CheckCommonConditions(element, map);
        }
    }
}
