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
            this.Table = node.table;
        }


        /// <summary>
        /// Gets an element that will be tested if it can be added to the result.
        /// </summary>
        /// <param name="element"> Element to be tested. </param>
        /// <param name="scope"> Scope of variables in search context. </param>
        /// <returns> True if element can be aplicable or false on refusal. </returns>
        public abstract bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used);


        /// <summary>
        /// Called by descendants. Checks conditions that are indifferent to the descendant type.
        /// Checks correctes of type of the aplied elements. 
        /// Also directs seting of map and used elements.
        /// </summary>
        /// <param name="element"> Elemented to be tested. </param>
        /// <param name="map"> Scope of variables in search context.</param>
        /// <param name="used"> Variables used in the search (dep. on if the match object is Edge or Vertex). </param>
        /// <returns>True if element can be aplicable or false on refusal.</returns>
        protected bool CheckCommonConditions(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            // Check type, comparing references to tables.
            if ((this.Table != null) && (this.Table != element.Table)) return false;

            // It is anonnymous, then it can match any vertex/edge.
            if (this.IsAnonnymous) return true;
            else  // it is a variable 
            {
                // Check if any element occupies variable rep. by this match object.
                if (map.TryGetValue(this.PositionOfRepeatedField, out Element tmpEl))
                {
                    // It contains el. 
                    // Check if the elemets are same.
                    if (tmpEl.ID != element.ID) return false;
                    else { /* Empty else -> returns true at the end */ }

                }
                else // The dict does not contain the element.
                {
                    // Check if the element is used for another variable.
                    if (used.ContainsKey(element)) return false;
                    // Add it to the map and to the used elements.
                    else
                    {
                        map.Add(this.PositionOfRepeatedField, element);
                        used.Add(element, true);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Unsets variable from scope and used elements.
        /// </summary>
        /// <param name="map"> Scope of the search algorithm. </param>
        /// <param name="used"> Used elements (edges/vertices. </param>
        public void UnsetVariable(Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (this.IsFirstAppereance && !this.IsAnonnymous)
            {
                if (map.TryGetValue(this.PositionOfRepeatedField, out Element tmpElement))
                {
                    map.Remove(this.PositionOfRepeatedField);
                    used.Remove(tmpElement);
                }
            }
        }

        /// <summary>
        /// Gets element corresponding to this match object.
        /// </summary>
        /// <param name="map"> Scope of the search algorithm. </param>
        /// <returns> Null if no element is used, else element of this match object. </returns>
        public Element GetVariable(Dictionary<int, Element> map)
        {
            if (this.IsAnonnymous) return null;
            else
            {
                if (this.IsFirstAppereance) return null;
                else if (map.ContainsKey(this.PositionOfRepeatedField))
                    return map[this.PositionOfRepeatedField];
                else throw new ArgumentException($"{ this.GetType()} Map does not contain desired variable.");
            }
        }


        /// <summary>
        /// Factory for base matches
        /// </summary>
        /// <param name="type"> Type of match node</param>
        /// <param name="node"> Prototype of the node </param>
        /// <param name="indexInMap"> Index of its variable in scope </param>
        /// <returns></returns>
        public static DFSBaseMatch CreateDFSBaseMatch(EdgeType edgeType, ParsedPatternNode node, int indexInMap, bool isFirst)
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

    class DFSVertexMatch : DFSBaseMatch
    {
        public DFSVertexMatch() : base()
        { }

        public DFSVertexMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is Vertex)) return false;
            else return CheckCommonConditions(element, map, used);
        }

    }

    abstract class DFSEdgeMatch : DFSBaseMatch
    {
        public DFSEdgeMatch() : base()
        { }

        public DFSEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public abstract EdgeType GetEdgeType();



    }

    class DFSInEdgeMatch : DFSEdgeMatch
    {
        public DFSInEdgeMatch() : base()
        { }
        public DFSInEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.InEdge;

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is InEdge)) return false;
            else return CheckCommonConditions(element, map, used);
        }

    }
    class DFSOutEdgeMatch : DFSEdgeMatch
    {
        public DFSOutEdgeMatch() : base()
        { }
        public DFSOutEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.OutEdge;

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is OutEdge)) return false;
            else return CheckCommonConditions(element, map, used);
        }

    }

    class DFSAnyEdgeMatch : DFSEdgeMatch
    {
        public DFSAnyEdgeMatch() : base()
        { }
        public DFSAnyEdgeMatch(ParsedPatternNode node, int indexInMap, bool isFirst) : base(node, indexInMap, isFirst)
        { }

        public override EdgeType GetEdgeType() => EdgeType.AnyEdge;

        public override bool Apply(Element element, Dictionary<int, Element> map, Dictionary<Element, bool> used)
        {
            if (element == null) return false;
            else if (!(element is Edge)) return false;
            else return CheckCommonConditions(element, map, used);
        }
    }


}
