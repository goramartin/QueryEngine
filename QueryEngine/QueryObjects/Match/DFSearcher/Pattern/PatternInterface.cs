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
        /// <summary>
        /// Try to match given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns> True if it can be matched otherwise false. </returns>
        bool Apply(Element element);

        /// <summary>
        /// Prepares next pattern chain when moving to the next pattern chain from the last match node of the current pattern chain.
        /// </summary>
        void PrepareNextSubPattern();

        /// <summary>
        /// Prepares previous pattern chain when returning from the first match node of current pattern chain.
        /// </summary>
        void PreparePreviousSubPattern();

        /// <summary>
        /// Prepares a next node for DFS forward recursion.
        /// </summary>
        void PrepareNextNode();
        /// <summary>
        /// Prepares a node for returning from recursion of DFS.
        /// </summary>
        void PreparePreviousNode();

        /// <summary>
        /// Returns if there are more match nodes in the current pattern chain.
        /// </summary>
        bool isLastNodeInCurrentPattern();
        /// <summary>
        /// Retusn whether there are more pattern chains left.
        /// </summary>
        bool isLastPattern();

        /// <summary>
        /// Returns current index of currently processed pattern chain.
        /// </summary>
        int CurrentPatternIndex { get; }
        /// <summary>
        /// Returns current match node position with refference to the current pattern.
        /// </summary>
        int CurrentMatchNodeIndex { get; }
        /// <summary>
        /// Returns current position with refference to the entire pattern.
        /// </summary>
        int OverAllIndex { get; }

        /// <summary>
        /// Returns a number of all pattern chains in the entire pattern.
        /// </summary>
        int PatternCount { get; }
        /// <summary>
        /// Returns a number of match nodes in the current pattern.
        /// </summary>
        int CurrentPatternCount { get; }
        /// <summary>
        /// Returns a number of match nodes in the entire pattern.
        /// </summary>
        int AllNodeCount { get; }

        Element[] GetMatchedVariables();
    }

    /// <summary>
    /// Interface neccessary for each DFS pattern.
    /// </summary>
    internal interface IDFSPattern : IPattern
    {
        /// <summary>
        ///  Gets starting element of the current chain.
        /// </summary>
        /// <returns> Null if anonymous/first appearance else element from scope. </returns>
        Element GetCurrentChainConnection();
        /// <summary>
        /// Gets starting element of the next chain.
        /// This method is called only when there is another pattern.
        /// If the next chain contains a variable that was already used it returns it from the scope.
        /// </summary>
        /// <returns>Null if anonymous/first appearance else element from scope. </returns>
        Element GetNextChainConnection();
        /// <summary>
        /// Returns a graph element to be matched.
        /// </summary>
        Type GetMatchType();
        /// <summary>
        /// Unsets current variable if set on the current chain node.
        /// </summary>
        void UnsetCurrentVariable();
        IDFSPattern Clone();
    }
}
