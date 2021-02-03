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
    /// Note that this pattern is very simple and in case the patterns are connected and shuffled, then the indeces might not be 
    /// visible to the user.
    /// Each variable is inserted only once despite possible multiple occurences of the same variable.
    /// </summary>
    internal class VariableMap : IEnumerable<KeyValuePair<string, Tuple<int, Table>>>
    {
     
        /// <summary>
        /// A map with information about defined variables.
        /// </summary>
        private Dictionary<string, Tuple<int, Table>> variableMap;
        
        
        public VariableMap() => this.variableMap = new Dictionary<string, Tuple<int, Table>>();
      
        /// <summary>
        /// Indexer to ease access with string keys to a map.
        /// </summary>
        /// <param name="str"> Name of variable. </param>
        /// <returns> Tuple with position of variable in result and its type. </returns>
        public Tuple<int, Table> this[string str]
        {
            get { return this.variableMap[str]; }
        }

        /// <summary>
        /// Adds variable to the dictionary.
        /// </summary>
        /// <param name="varName"> Name of variable to insert </param>
        /// <param name="position"> Position in flattened pattern </param>
        /// <param name="table"> Type of inserted variable </param>
        public void AddVariable(string varName, int position, Table table)
        {
            if (this.variableMap.ContainsKey(varName))
                throw new ArgumentException($"{this.GetType()} Variable is already in the Score. Name = {varName}.");
            else this.variableMap.Add(varName, Tuple.Create<int, Table>(position, table));
        }

        /// <summary>
        /// Returns positon of variable based on the name of the variable.
        /// </summary>
        /// <param name="name"> Variable to be searched for in Dictionary. </param>
        /// <returns> Position of variable in flattened pattern </returns>
        public int GetVariablePosition(string name)
        {
            if (this.variableMap.TryGetValue(name, out var tuple)) return tuple.Item1;
            else return -1;
        }


        /// <summary>
        /// Copy of Dictionary method TryGetValue
        /// </summary>
        /// <param name="name"> Key </param>
        /// <param name="tuple"> Value </param>
        /// <returns> True on retrieval of value </returns>
        public bool TryGetValue(string name, out Tuple<int, Table> tuple)
        {
            return this.variableMap.TryGetValue(name, out tuple);
        }

        /// <returns> Count of variables. </returns>
        public int GetCount() => this.variableMap.Count;

        public IEnumerator<KeyValuePair<string, Tuple<int,Table>>> GetEnumerator()
        {
            return this.variableMap.GetEnumerator();
        }

        /// <summary>
        /// Calls generic method of get enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
