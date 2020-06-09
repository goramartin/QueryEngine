/*! \file 
  
  This file includes definitions of match classes that form a pattern chains.
  Each class represents one object that can be matched during search algorithm.
  e.g. vertex and specific edge type.
  
  Each class has got a method that tests element if it can be matched. If the element can be matched
  they also make neccessary adjustments to the structures passed to those methods.
  This class is directly connected to the dfs pattern class.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class representing single step in search algorithm.
    /// Every step, an element is tried to be applied through the apply method.
    /// Method apply returns true if the element can be added to final result.
    /// Descendants share certain conditions when applying such as Type, Variable name, Edge type...
    /// </summary>
    abstract class DFSBaseMatch
    {
        // The match is anonymous if it does not represent any variable.
        readonly bool IsAnonnymous;
        // Is true if the match object represents a variable that has it is first appereance.
        readonly bool IsFirstAppereance;
        // Represents index in scope if it is not anonymous.
        readonly int PositionOfRepeatedField;
        readonly Table Table;

        public DFSBaseMatch()
        {
            this.IsAnonnymous = true;
            this.PositionOfRepeatedField = -1;
            this.Table = null;
            this.IsFirstAppereance = true;
        }

        /// <summary>
        /// Constructor for each DFS Match object.
        /// </summary>
        /// <param name="node"> Node containing data of the match object. </param>
        /// <param name="indexInMap"> Index in the map of variables. (-1 if the the variable is anonymous.) </param>
        /// <param name="isFirst"> Indicates whether its first appearance of the variable. </param>
        protected DFSBaseMatch(ParsedPatternNode node, int indexInMap, bool isFirst)
        {
            if (indexInMap != -1) this.IsAnonnymous = false;
            else this.IsAnonnymous = true;

            this.IsFirstAppereance = isFirst;
            this.PositionOfRepeatedField = indexInMap;
            this.Table = node.Table;
        }


        /// <summary>
        /// Gets an element that will be tested if it can be added to the result.
        /// </summary>
        /// <param name="element"> Element to be tested. </param>
        /// <param name="map"> Scope of variables in search context. </param>
        /// <returns> True if element can be aplicable or false on refusal. </returns>
        public abstract bool Apply(Element element, Element[] map);


        /// <summary>
        /// Called by descendants. Checks conditions that are indifferent to the descendant type.
        /// Checks correctes of type of the aplied elements. 
        /// Also directs seting of map and used elements.
        /// </summary>
        /// <param name="element"> Elemented to be tested. </param>
        /// <param name="map"> Scope of variables in search context.</param>
        /// <returns>True if element can be aplicable or false on refusal.</returns>
        protected bool CheckCommonConditions(Element element, Element[] map)
        {
            // Check type, comparing references to tables.
            if ((this.Table != null) && (this.Table != element.Table)) return false;

            // It is anonnymous, then it can match any vertex/edge.
            if (this.IsAnonnymous) return true;
            else  // it is a variable 
            {
                // Check if any element occupies variable rep. by this match object.
                if (map[this.PositionOfRepeatedField] != null)
                {
                    // It contains el. 
                    // Check if the two elements are same.
                    if (map[this.PositionOfRepeatedField].ID != element.ID) return false;
                    else { /* Empty else -> it returns true at the end */ }

                } // The variable is not occupied by the element -> add the element
                else map[this.PositionOfRepeatedField] = element;
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
            if (!this.IsAnonnymous && this.IsFirstAppereance)
                map[this.PositionOfRepeatedField] = null;
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
            if (this.IsAnonnymous) return null;
            else return map[this.PositionOfRepeatedField];
        }


        /// <summary>
        /// Factory for base matches
        /// </summary>
        /// <param name="edgeType"> Type of edge node</param>
        /// <param name="node"> Prototype of the node </param>
        /// <param name="indexInMap"> Index of its variable in scope </param>
        /// <param name="isFirst"> If the match node represents variable that appears for the first time.</param>
        /// <returns> Base match node. </returns>
        public static DFSBaseMatch DFSBaseMatchFactory(EdgeType edgeType, ParsedPatternNode node, int indexInMap, bool isFirst)
        {
            switch (edgeType)
            {
                case EdgeType.NotEdge:
                    return new DFSVertexMatch(node, indexInMap, isFirst);
                case EdgeType.InEdge:
                    return new DFSInEdgeMatch(node, indexInMap, isFirst);
                case EdgeType.OutEdge:
                    return new DFSOutEdgeMatch(node, indexInMap, isFirst);
                case EdgeType.AnyEdge:
                    return new DFSAnyEdgeMatch(node, indexInMap, isFirst);
                default:
                    throw new ArgumentException($"Trying to create DFS Match type that does not exit.");
            }
        }
    }
}
