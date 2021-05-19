/*! \file
  This file includes definitions of visitors used to collect data from created parsed trees. 
  Each query object has its own visitor.
  The Visitor iterates over nodes defined in ParseTreeNodes.cs
 */

namespace QueryEngine
{
    /// <typeparam name="T"> Object built after parsing </typeparam>
    internal interface IVisitor<T>
    {
        T GetResult();
        void Visit(SelectNode node);
        void Visit(SelectPrintTermNode node);
        
            
        void Visit(MatchNode node);
        void Visit(MatchDividerNode node);
        void Visit(VertexNode node);
        void Visit(InEdgeNode node);
        void Visit(OutEdgeNode node);
        void Visit(AnyEdgeNode node);
        void Visit(MatchVariableNode node);

        void Visit(OrderByNode node);
        void Visit(OrderTermNode node);

        void Visit(GroupByNode node);
        void Visit(GroupByTermNode node);

        
        void Visit(ExpressionNode node);
        void Visit(VariableNode node);
        void Visit(IdentifierNode node);
        void Visit(AggregateFuncNode node);

    }

}
