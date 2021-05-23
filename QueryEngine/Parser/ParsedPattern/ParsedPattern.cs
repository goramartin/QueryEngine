/*! \file
This file includes definitions of parsed patterns that are later on used for creating pattern.
PGQL syntax for match section is done via "chains" connected with commas, e.g. MATCH (x) - (y), (y) - (p).
The first chain is (x) - (y) and the second one is (y) - (p).
The parsed pattern is a class that encapsulates one chain that consists of the parsed pattern nodes and allows certain operations, such as splitting.
This allows us to work with the patterns more easily.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// A class used to shallow parsing of match expression from the query.
    /// A pattern contains nodes with their corresponding attributes collected during parsing.
    /// Can be splited by a split variable.
    /// The split creates two separate patterns.
    /// </summary>
    internal sealed class ParsedPattern
    {
        public List<ParsedPatternNode> pattern;
        public string splitBy;

        public ParsedPattern()
        {
            this.pattern = new List<ParsedPatternNode>();
            this.splitBy = null;
        }

        public ParsedPattern(List<ParsedPatternNode> pattern)
        {
            this.pattern = pattern;
            this.splitBy = null;
        }

        public void AddParsedPatternNode(ParsedPatternNode node)
        {
            this.pattern.Add(node);
        }

        public int GetCount() => this.pattern.Count;
        public ParsedPatternNode GetLastParsedPatternNode() => this.pattern[this.pattern.Count - 1];

        /// <summary>
        /// Searches for the same variable inside two Parsed Patterns.
        /// </summary>
        /// <param name="other"> Parsed Pattern to be searched for similar variables. </param>
        /// <param name="name"> A name of the first same variable. </param>
        /// <returns> True if found, false when not found the same variable. Returns the first found variable.</returns>
        public bool TryFindEqualVariable(ParsedPattern other, out string name)
        {
            // For each variable in current pattern check equality for variables in the other pattern.
            for (int k = 0; k < this.pattern.Count; k++)
            {
                for (int l = 0; l < other.pattern.Count; l++)
                {
                    // Found matching variable.
                    if (this.pattern[k].Equals(other.pattern[l]))
                    {
                        name = this.pattern[k].Name;
                        return true;
                    }
                }
            }
            name = "";
            return false;
        }

        /// <summary>
        /// Splits a ParsedPattern node into two ParsedPatterns. 
        /// An instance on which we split, the pattern is reduced from the beginning.
        /// The newly build pattern is build in a reversed order.
        /// For example: (a) - (b) - (c) splited by var. b results in two patterns (b) - (a) , (b) - (c).
        /// Split is done only if the splitVariable is not located on the first index of the pattern.
        /// If the variable is located in the end of the pattern, the pattern is only reversed in place.
        /// </summary>
        /// <returns> Returns the part before split variable in reverse order or null if the split variable is the first one in the pattern. </returns>
        public ParsedPattern TrySplitParsedPattern()
        {
            int i = this.FindIndexOfSplitVariable();
            if (i == 0 || i == -1) return null;
            else
            {
                if (i == this.pattern.Count - 1) return this.ReverseInPlace(i);
                else return this.SplitIntoTwo(i);
            }

        }

        /// <summary>
        /// This method is called if the split index if the last index of the pattern.
        /// Then the spilliting is uneccessary and the chain can be reversed in place.
        /// Note we are removing former nodes from the end of the List, and add the reversed ones to the end.
        /// The size of array after one iteration stays the same as before calling the method.
        /// </summary>
        /// <param name="lastNodeinPatternIndex"> An index of split variable which is the last node in the pattern. </param>
        /// <returns> The same instance as the caller. </returns>
        private ParsedPattern ReverseInPlace(int lastNodeinPatternIndex)
        {
            for (int j = lastNodeinPatternIndex - 1; j >= 0; j--)
            {
                var tmp = this.pattern[j];
                this.pattern.RemoveAt(j);
                this.pattern.Add(tmp.CloneReverse());
            }
            return null;
        }

        /// <summary>
        /// Splits the pattern of this instance into two parts. 
        /// First part starts with variable on the split index and contains reversed pattern from the index to the beginning
        /// of the pattern ( reversed edges as well ).
        /// The second part starts with variable on the split index and contains former pattern from the index to the end of the pattern
        /// unchanged.
        /// Note that the last part stays in the instance while the first part is used to create a new instance of Parsed pattern
        /// Example:
        /// For parsed pattern: (x) -o (z) o- (p) - () where the split variable is p. The result looks:
        /// The first part = (p) -o (z) o- (x), the second part = (p) - () 
        /// </summary>
        /// <param name="splitVariableIndex"> An index of split variable </param>
        /// <returns> A new instance of Parsed Pattern </returns>
        private ParsedPattern SplitIntoTwo(int splitVariableIndex)
        {
            var firstPart = new List<ParsedPatternNode>();
            for (int j = 0; j <= splitVariableIndex; j++)
            {
                firstPart.Insert(0, this.pattern[0].CloneReverse());
                if (j == splitVariableIndex) break;
                else this.pattern.RemoveAt(0);
            }
            return new ParsedPattern(firstPart);
        }


        /// <summary>
        /// Finds an index of a split variable.
        /// </summary>
        /// <returns> An index of the split variable, -1 if there is not one. </returns>
        private int FindIndexOfSplitVariable()
        {
            if (this.splitBy == null) return -1;
           
            // Find an index of splitVariable.
            for (int i = 0; i < this.GetCount(); i++)
                if (this.splitBy == this.pattern[i].Name) return i;
            
            return -1;
        }

    }

}
