using System;

namespace QueryEngine
{
    /// <summary>
    /// An interface neccessary for each DFS pattern.
    /// </summary>
    internal interface IDFSPattern : IPattern
    {
        /// <summary>
        ///  Gets a starting element of the current chain.
        /// </summary>
        /// <returns> Null if anonymous/first appearance else element from scope. </returns>
        Element GetCurrentChainConnection();
        /// <summary>
        /// Gets a starting element of the next chain.
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
        /// Unsets the current variable if set on the current chain node.
        /// </summary>
        void UnsetCurrentVariable();
        IDFSPattern Clone();
    }
}
