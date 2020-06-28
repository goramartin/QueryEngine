/*! \file
 
    This file inludes definition if an interface to a dfs pattern.
 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
    /// <summary>
    /// Basic interface for each pattern.
    /// </summary>
    internal interface IPattern
    {
        bool Apply(Element element);

        void PrepareNextSubPattern();
        void PreparePreviousSubPattern();

        void PrepareNextNode();
        void PreparePreviousNode();

        bool isLastNodeInCurrentPattern();
        bool isLastPattern();


        int CurrentPatternIndex { get; }
        int CurrentMatchNodeIndex { get; }
        int OverAllIndex { get; }

        int PatternCount { get; }
        int CurrentPatternCount { get; }

        int AllNodeCount { get; }

        Element[] GetMatchedVariables();
    }

    /// <summary>
    /// Interface neccessary for each DFS pattern.
    /// </summary>
    internal interface IDFSPattern : IPattern
    {
        Element GetCurrentChainConnection();
        Element GetNextChainConnection();
        Type GetMatchType();
        void UnsetCurrentVariable();
        IDFSPattern Clone();
    }
}
