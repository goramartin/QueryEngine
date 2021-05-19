using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{

    /// <summary>
    /// VariableMap represents a map of variables in the whole query.
    /// Variables are given indeces that correspond to the position in a result row + their type if included.
    /// For example, let the match clause be (x) -> (z), in the query there are two variables.
    /// The results of the matching algorithm will take a form of table, where first column represents 
    /// the x variable and the second column represents the z variable. So the indeces for the variables of x and z are
    /// 0 and 1, because when accessing x variable the 0 th column is used and vice versa.
    /// 
    /// Note that in this example the pattern is very simple and in case the patterns are connected and shuffled, then the indeces of variable might not be 
    /// transparent to the user.
    /// Each variable is inserted only once despite possible multiple occurences of the same variable.
    /// </summary>
    internal class VariableMap : IEnumerable<KeyValuePair<string, Tuple<int, Table>>>
    {
     
        /// <summary>
        /// A map with information about defined variables.
        /// Tuple contains an index of the variable and possibly it's type if it is stated in the query.
        /// </summary>
        private Dictionary<string, Tuple<int, Table>> variableMap;
        
        public VariableMap() => this.variableMap = new Dictionary<string, Tuple<int, Table>>();
      
        /// <summary>
        /// Indexer to ease access with string keys to a map.
        /// </summary>
        /// <param name="str"> A name of a variable. </param>
        /// <returns> A tuple witha  position of a variable in the result and its type. </returns>
        public Tuple<int, Table> this[string str]
        {
            get { return this.variableMap[str]; }
        }

        /// <summary>
        /// Adds a variable to the map.
        /// </summary>
        /// <param name="varName"> A name of a variable to insert. </param>
        /// <param name="position"> A position in the flattened pattern. </param>
        /// <param name="table"> Type of inserted variable. </param>
        public void AddVariable(string varName, int position, Table table)
        {
            if (this.variableMap.ContainsKey(varName))
                throw new ArgumentException($"{this.GetType()} Variable is already in the Score. Name = {varName}.");
            else this.variableMap.Add(varName, Tuple.Create<int, Table>(position, table));
        }

        /// <summary>
        /// Returns a positon of a variable based on the name of the variable.
        /// </summary>
        /// <param name="name"> A variable to be searched for in the map. </param>
        /// <returns> A position of a variable in the flattened pattern. </returns>
        public int GetVariablePosition(string name)
        {
            if (this.variableMap.TryGetValue(name, out var tuple)) return tuple.Item1;
            else return -1;
        }

        public bool TryGetValue(string name, out Tuple<int, Table> tuple)
        {
            return this.variableMap.TryGetValue(name, out tuple);
        }

        public int GetCount() => this.variableMap.Count;

        public IEnumerator<KeyValuePair<string, Tuple<int,Table>>> GetEnumerator()
        {
            return this.variableMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
