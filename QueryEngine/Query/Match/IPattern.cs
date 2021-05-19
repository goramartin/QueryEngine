namespace QueryEngine 
{
    /// <summary>
    /// A vasic interface for each pattern.
    /// </summary>
    internal interface IPattern
    {
        /// <summary>
        /// Try to match the given element.
        /// </summary>
        /// <returns> True if it can be matched otherwise false. </returns>
        bool Apply(Element element);

        /// <summary>
        /// Prepares the next pattern chain when moving to the next pattern chain from the last match node of the current pattern chain.
        /// </summary>
        void PrepareNextSubPattern();

        /// <summary>
        /// Prepares the previous pattern chain when returning from the first match node of the current pattern chain.
        /// </summary>
        void PreparePreviousSubPattern();

        /// <summary>
        /// Prepares the next node for DFS forward recursion.
        /// </summary>
        void PrepareNextNode();
        /// <summary>
        /// Prepares a node for returning from recursion of DFS.
        /// </summary>
        void PreparePreviousNode();

        /// <summary>
        /// Returns false if the current node in the pattern is not the last one.
        /// </summary>
        bool IsLastNodeInCurrentPattern();
        /// <summary>
        /// Returns false if the current pattern is not the last one.
        /// </summary>
        bool IsLastPattern();

        /// <summary>
        /// Returns a current index of the currently processed pattern chain.
        /// </summary>
        int CurrentPatternIndex { get; }
        /// <summary>
        /// Returns a current match node position with refference to the current pattern.
        /// </summary>
        int CurrentMatchNodeIndex { get; }
        /// <summary>
        /// Returns a current position with refference to the entire pattern.
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
