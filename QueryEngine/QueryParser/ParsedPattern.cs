
/*! \file
  This file includes definitions of parsed pattern that is later on used for creating pattern 
  that is used in search algorithm.
  PGQL syntax for match section is done via "chains" connected with commas.
  e.g. MATCH (x) - (y), (y) - (p)
  First chain is (x) - (y) and the second one is (y) - (p).
  Parsed pattern is class that encapsulated one chain of the pattern and allows certain operations 
  to make working with the pattern more easily. Such as splitting.
  Parsed pattern nodes are used to form a specified object of the chains e.g vertices and edges.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class used to shallow parsing of match expression.
    /// Pattern contains single nodes with their corresponding attributes collected when parsed.
    /// Can be splited by a certain parsed pattern node which contains given string variable
    /// Is used when creating specialised pattern, such as DFS.
    /// </summary>
    class ParsedPattern
    {
        public List<ParsedPatternNode> Pattern;
        public string splitBy;

        public ParsedPattern()
        {
            this.Pattern = new List<ParsedPatternNode>();
            this.splitBy = null;
        }

        public ParsedPattern(List<ParsedPatternNode> pattern)
        {
            this.Pattern = pattern;
            this.splitBy = null;
        }

        public void AddParsedPatternNode(ParsedPatternNode node)
        {
            this.Pattern.Add(node);
        }

        public int GetCount() => this.Pattern.Count;
        public ParsedPatternNode GetLastParsedPatternNode() => this.Pattern[this.Pattern.Count - 1];

        /// <summary>
        /// Searches for the same variable inside two Parsed Patterns.
        /// </summary>
        /// <param name="other"> Parsed Pattern to be searched for similar variables. </param>
        /// <param name="name"> Name of the first same variable. </param>
        /// <returns> True if found, False when not found the same variable. </returns>
        public bool TryFindEqualVariable(ParsedPattern other, out string name)
        {
            // For each variable in current pattern check equality for variables in the other pattern
            for (int k = 0; k < this.Pattern.Count; k++)
            {
                for (int l = 0; l < other.Pattern.Count; l++)
                {
                    // Found matching variable
                    if (this.Pattern[k].Equals(other.Pattern[l]))
                    {
                        name = this.Pattern[k].name;
                        return true;
                    }
                }
            }
            name = "";
            return false;
        }

        /// <summary>
        /// Splits ParsedPattern node into two ParsedPatterns. 
        /// Instance on which we split, the pattern is reduced from the beginning.
        /// The new build pattern (the nodes taken into account from the reduction from the instance) is build in a reversed order.
        /// For example: (a) - (b) - (c) splited by var. b == (b) - (a) , (b) - (c).
        /// Split is done only if the splitVariable is not located in on the first index of the pattern.
        /// If the variable is located in the end of the pattern, the pattern is only reversed inplace.
        /// </summary>
        /// <returns> Returns the part before split variable (reverse order) or null if it is the first one. </returns>
        public ParsedPattern SplitParsedPattern()
        {
            int i = this.FindIndexOfSplitVariable();
            if (i == 0 || i == -1) return null;
            else
            {
                if (i == this.Pattern.Count - 1) return this.SplitInPlace(i);
                else return this.SplitIntoTwo(i);
            }

        }

        /// <summary>
        /// Intead of spliting into one normal part and the other empty part.
        /// We only reverse order of the nodes in the pattern of this instance.
        /// </summary>
        /// <param name="i"> Index of split variable </param>
        /// <returns> The same instance as the caller. </returns>
        private ParsedPattern SplitInPlace(int i)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                // i is index of the last node
                // Invariant length of pattern stays same
                var tmp = this.Pattern[j];
                this.Pattern.RemoveAt(j);

                if (tmp.isVertex) this.Pattern.Add(tmp);
                else this.Pattern.Add(tmp.CloneReverse());
            }
            return null;
        }

        /// <summary>
        /// Splits the pattern of this instance into two
        /// </summary>
        /// <param name="i"> Index of split variable </param>
        /// <returns> New instance of Parsed Pattern </returns>
        private ParsedPattern SplitIntoTwo(int i)
        {
            var firstPart = new List<ParsedPatternNode>();
            for (int j = 0; j <= i; j++)
            {
                firstPart.Insert(0, this.Pattern[0].CloneReverse());
                if (j == i) break;
                else this.Pattern.RemoveAt(0);
            }
            return new ParsedPattern(firstPart);
        }


        /// <summary>
        /// Finds index of split variable.
        /// </summary>
        /// <returns> Index of split variable, -1 for not containing </returns>
        private int FindIndexOfSplitVariable()
        {
            if (this.splitBy == null) return -1;
            //Find index of splitVariable
            for (int i = 0; i < this.GetCount(); i++)
            {
                if (this.splitBy == this.Pattern[i].name) return i;
            }
            return -1;
        }

    }


    /// <summary>
    /// Represents single Node when parsing match expression.
    /// There is no need to create another type just for edge type as those will be created later.
    /// Caries information about match node. 
    /// </summary>
    class ParsedPatternNode
    {
        public bool isAnonymous;
        public bool isVertex;
        public Table table;
        public EdgeType edgeType;
        public string name;

        public ParsedPatternNode()
        {
            this.table = null;
            this.name = null;
            this.isVertex = true;
            this.isAnonymous = true;
        }

        public bool IsAnonymous() => this.isAnonymous;
        public bool IsVertex() => this.isVertex;
        public Table GetTable() => this.table;
        public EdgeType GetEdgeType() => this.edgeType;
        public string GetName() => this.name;

        /// <summary>
        /// Creates copy of instance, reverses edges.
        /// </summary>
        /// <returns> Copy of this instance. </returns>
        public ParsedPatternNode CloneReverse()
        {
            var clone = new ParsedPatternNode();

            clone.isVertex = this.isVertex;
            clone.name = this.name;
            clone.table = this.table;
            clone.isAnonymous = this.isAnonymous;

            if (this.edgeType == EdgeType.InEdge) clone.edgeType = EdgeType.OutEdge;
            else if (this.edgeType == EdgeType.OutEdge) clone.edgeType = EdgeType.InEdge;
            else clone.edgeType = this.edgeType;
            return clone;
        }

        public override bool Equals(object obj)
        {
            if (obj is ParsedPatternNode)
            {
                var o = obj as ParsedPatternNode;
                if (this.isAnonymous && o.isAnonymous) return false;

                if (this.name != o.name) return false;
                else if (this.isVertex != o.isVertex) return false;
                //referrence to the same object is valid here
                else if (this.table != o.table) return false;
                else if (this.edgeType != o.edgeType) return false;
                return true;
            }
            return false;
        }
    }
}
