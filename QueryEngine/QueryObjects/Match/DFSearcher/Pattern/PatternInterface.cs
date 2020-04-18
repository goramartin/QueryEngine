﻿/*! \file
 
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
    interface IPattern
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

        Dictionary<int, Element> GetMatchedVariables();
    }

    /// <summary>
    /// Interface neccessary for each DFS pattern.
    /// </summary>
    interface IDFSPattern : IPattern
    {
        Element GetCurrentChainConnection();
        Element GetNextChainConnection();
        EdgeType GetEdgeType();
        void UnsetCurrentVariable();
        IDFSPattern Clone();
    }
}