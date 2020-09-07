/*! \file
This file includes definitions of a parsed pattern that is later on used for creating pattern 
that is, at the end, used in the search algorithm.
PGQL syntax for match section is done via "chains" connected with commas.
e.g. MATCH (x) - (y), (y) - (p)
First chain is (x) - (y) and the second one is (y) - (p).
Parsed pattern is class that encapsulated one chain that consists of the parsed pattern nodes and allows certain operations 
to allow working with the pattern more easily. Such as splitting.
Spliting is used to make search pattern linear (it matches only forward) and it helps connect interconnected patterns through out 
all other chains.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class used to shallow parsing of match expression from user inputted query.
    /// Pattern contains single nodes with their corresponding attributes collected when parsed.
    /// Can be splited by a split variable if set and creating two separate patterns.
    /// </summary>
    internal sealed class ParsedPattern
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
        /// <returns> True if found, False when not found the same variable. Returns the first found variable.</returns>
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
                        name = this.Pattern[k].Name;
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
        /// Split is done only if the splitVariable is not located on the first index of the pattern.
        /// If the variable is located in the end of the pattern, the pattern is only reversed in place.
        /// </summary>
        /// <returns> Returns the part before split variable (reverse order) or null if it is the first one. </returns>
        public ParsedPattern TrySplitParsedPattern()
        {
            int i = this.FindIndexOfSplitVariable();
            if (i == 0 || i == -1) return null;
            else
            {
                if (i == this.Pattern.Count - 1) return this.ReverseInPlace(i);
                else return this.SplitIntoTwo(i);
            }

        }

        /// <summary>
        /// This method is called if the split index if the last index of the pattern.
        /// Then the spilliting is uneccessary and the chain can be reversed in place.
        /// Note we are removing former nodes from the end of the list, and add the reversed ones to the end.
        /// The size of array after one iteration stays the same as before calling the method.
        /// </summary>
        /// <param name="lastNodeinPatternIndex"> Index of split variable which is last node in pattern. </param>
        /// <returns> The same instance as the caller. </returns>
        private ParsedPattern ReverseInPlace(int lastNodeinPatternIndex)
        {
            for (int j = lastNodeinPatternIndex - 1; j >= 0; j--)
            {
                var tmp = this.Pattern[j];
                this.Pattern.RemoveAt(j);
                this.Pattern.Add(tmp.CloneReverse());
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
        /// <param name="splitVariableIndex"> Index of split variable </param>
        /// <returns> New instance of Parsed Pattern </returns>
        private ParsedPattern SplitIntoTwo(int splitVariableIndex)
        {
            var firstPart = new List<ParsedPatternNode>();
            for (int j = 0; j <= splitVariableIndex; j++)
            {
                firstPart.Insert(0, this.Pattern[0].CloneReverse());
                if (j == splitVariableIndex) break;
                else this.Pattern.RemoveAt(0);
            }
            return new ParsedPattern(firstPart);
        }


        /// <summary>
        /// Finds index of a split variable. The split variable is used to split the pattern into two parts.
        /// </summary>
        /// <returns> Index of the split variable, -1 for not containing any split variable.</returns>
        private int FindIndexOfSplitVariable()
        {
            if (this.splitBy == null) return -1;
           
            //Find index of splitVariable
            for (int i = 0; i < this.GetCount(); i++)
                if (this.splitBy == this.Pattern[i].Name) return i;
            
            return -1;
        }

    }

}
