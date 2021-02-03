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
        bool IsLastNodeInCurrentPattern();
        /// <summary>
        /// Retusn whether there are more pattern chains left.
        /// </summary>
        bool IsLastPattern();

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

}
