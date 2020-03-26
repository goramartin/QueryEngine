﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// VariableMap represents a map of variables in the whole query during pattern matching.
    /// When the Pattern is flattned the integer value that resides on given variable name
    /// corresponds to the index in the flattened pattern.
    /// That is done because we need to retrieve the variable from the matched elements from within the flattened pattern.
    /// 
    /// Each variable is inserted only once despite possible multiple occurences of the same variable.
    /// The main purpose of the variableMap is to obtain Elements that is the repetition does not change the desired value.
    /// </summary>
    class VariableMap
    {
        private Dictionary<string, Tuple<int, Table>> variableMap;
        public VariableMap(Dictionary<string, Tuple<int, Table>> sv) => this.variableMap = sv;
        public VariableMap() => this.variableMap = new Dictionary<string, Tuple<int, Table>>();

        public Dictionary<string, Tuple<int, Table>> GetvariableMapVariables() => this.variableMap;

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

        public int GetCount() => this.variableMap.Count;
    }
}
