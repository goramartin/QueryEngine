/*! \file 
This file includes definitions of match classes that form pattern chains.
Each class represents one graph element that can be matched during search algorithm.
e.g. vertex and specific edge type.
  
Each class has got a method that tests element if it can be matched. If the element can be matched
they also make neccessary adjustments to the structures passed to those methods.
This class is directly connected to the dfs pattern class.
 */

using System;

namespace QueryEngine
{
    /// <summary>
    /// Class representing single step in search algorithm.
    /// Every step, an element is tried to be applied through the apply method.
    /// Method apply returns true if the element can be added to final result.
    /// Descendants share certain conditions when applying such as Type, Variable name, Edge type...
    /// </summary>
    internal abstract class DFSBaseMatch
    {
        /// <summary>
        /// The match is anonymous if it does not represent any variable.
        /// </summary>
        readonly bool isAnonnymous;
        /// <summary>
        /// Is true if the match object represents a variable that has it is first appereance. 
        /// </summary>
        readonly bool isFirstAppereance;
        /// <summary>
        /// Represents index in scope if it is not anonymous. 
        /// </summary>
        readonly int positionOfRepeatedField;
        /// <summary>
        /// Type of a table of the element being matched.
        /// </summary>
        readonly Table table;
        /// <summary>
        /// Type of graph element to be matched. Its faster to store the information directly then call virtual methods.
        /// The pattern is never really long, so the memory overhead is negligible.
        /// </summary>
        readonly Type matchingType;

        public DFSBaseMatch()
        {
            this.isAnonnymous = true;
            this.positionOfRepeatedField = -1;
            this.table = null;
            this.isFirstAppereance = true;
        }

        /// <summary>
        /// Constructor for each DFS Match object.
        /// </summary>
        /// <param name="node"> Node containing data of the match object. </param>
        /// <param name="indexInMap"> Index in the map of variables. (-1 if the the variable is anonymous.) </param>
        /// <param name="isFirst"> Indicates whether its first appearance of the variable. </param>
        /// <param name="matchingType"> Type of matching graph element. </param>
        protected DFSBaseMatch(ParsedPatternNode node, int indexInMap, bool isFirst, Type matchingType)
        {
            this.matchingType = matchingType;
            if (indexInMap != -1) this.isAnonnymous = false;
            else this.isAnonnymous = true;

            this.isFirstAppereance = isFirst;
            this.positionOfRepeatedField = indexInMap;
            this.table = node.Table;
        }

        /// <summary>
        /// Gets an element that will be tested if it can be added to the result.
        /// Checks correctes of type, if stated, of the aplied elements. 
        /// When this match object represents variable, it checks whether the element is the same if the variable is taken
        /// or just sets the element to be the variable.
        /// Note: The element is never null and always the correct type. => must be ensured by matcher.
        /// </summary>
        /// <param name="element"> Elemented to be tested. </param>
        /// <param name="map"> Scope of variables in search context.</param>
        /// <returns>True if element can be aplicable or false on refusal.</returns>
        public bool Apply(Element element, Element[] map)
        {
            // Check type, comparing references to tables.
            if ((this.table != null) && (this.table != element.Table)) return false;

            // It is anonnymous, then it can match any vertex/edge.
            if (this.isAnonnymous) return true;
            else  // it is a variable 
            {
                // Check if any element occupies variable rep. by this match object.
                if (map[this.positionOfRepeatedField] != null)
                {
                    // It contains el. 
                    // Check if the two elements are same.
                    if (map[this.positionOfRepeatedField].ID != element.ID) return false;
                    else { /* Empty else -> it returns true at the end */ }

                } // The variable is not occupied by the element -> add the element
                else map[this.positionOfRepeatedField] = element;
            }

            return true;
        }

        /// <summary>
        /// Unsets variable from scope.
        /// It checks for anonoymous to avoid uneccessary dict access,
        /// and it checks for first appearance to avoid unseting variable while it is still used.
        /// </summary>
        /// <param name="map"> Scope of the search algorithm. </param>
        public void UnsetVariable(Element[] map)
        {
            if (!this.isAnonnymous && this.isFirstAppereance)
                map[this.positionOfRepeatedField] = null;
        }

        /// <summary>
        /// Gets element corresponding to this match object.
        /// If the match node is anonymous, it cannot access any variable.
        /// This 
        /// </summary>
        /// <param name="map"> Scope of the search algorithm. </param>
        /// <returns> Null if no element is used, else element of this match object. </returns>
        public Element GetVariable(Element[] map)
        {
            if (this.isAnonnymous) return null;
            else return map[this.positionOfRepeatedField];
        }

        /// <summary>
        /// Returns type of graph element to be matched.
        /// This is faster then calling virtual methods and the memory overhead is small because the instances of types
        /// are created when starting the application.
        /// </summary>
        /// <returns> Type of graph element to be matched. </returns>
        public Type GetMatchType()
        {
            return this.matchingType;
        }


        /// <summary>
        /// Factory for base matches
        /// </summary>
        /// <param name="node"> Prototype of the node </param>
        /// <param name="indexInMap"> Index of its variable in scope </param>
        /// <param name="isFirst"> If the match node represents variable that appears for the first time.</param>
        /// <returns> Base match node. </returns>
        public static DFSBaseMatch DFSBaseMatchFactory(ParsedPatternNode node, int indexInMap, bool isFirst)
        {
            Type nodeType = node.GetType();
            if (nodeType == typeof(VertexParsedPatternNode)) return new DFSVertexMatch(node, indexInMap, isFirst);
            if (nodeType == typeof(InEdgeParsedPatternNode)) return new DFSInEdgeMatch(node, indexInMap, isFirst);
            if (nodeType == typeof(OutEdgeParsedPatternNode)) return new DFSOutEdgeMatch(node, indexInMap, isFirst);
            if (nodeType == typeof(AnyEdgeParsedPatternNode)) return new DFSAnyEdgeMatch(node, indexInMap, isFirst);
            else throw new ArgumentException($"Trying to create DFS Match type that does not exit.");
        }
    }
}
