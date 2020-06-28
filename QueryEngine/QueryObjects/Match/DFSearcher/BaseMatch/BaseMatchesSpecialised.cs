/*! \file
 
    This file include definitions of a specialised matches used to form a dfs pattern.
    There are a vertex and edges match classes. 
 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    
    internal sealed class DFSVertexMatch : DFSBaseMatch
    {
        public DFSVertexMatch() : base()
        { }

        public DFSVertexMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst, typeof(Vertex))
        { }

    }

   
    internal sealed class DFSInEdgeMatch : DFSBaseMatch
    {
        public DFSInEdgeMatch() : base()
        { }
        public DFSInEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst, typeof(InEdge))
        { }

    }

   
    internal sealed class DFSOutEdgeMatch : DFSBaseMatch
    {
        public DFSOutEdgeMatch() : base()
        { }
        public DFSOutEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst, typeof(OutEdge))
        { }

    }

    internal sealed class DFSAnyEdgeMatch : DFSBaseMatch
    {
        public DFSAnyEdgeMatch() : base()
        { }
        public DFSAnyEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst, typeof(Edge))
        { }

    }
}
