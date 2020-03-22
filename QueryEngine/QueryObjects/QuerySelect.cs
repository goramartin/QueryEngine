using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Select represents list of variables to be printed.
    /// List of select variables contains names and proprty names to be printed from the result.
    /// </summary>
    class SelectObject
    {
        private List<SelectVariable> selectVariables;


        /// <summary>
        /// Creates Select expression
        /// </summary>
        /// <param name="tokens"> Tokens to be parsed. (Expecting first token to be a Select token.)</param>
        public SelectObject(List<Token> tokens)
        {
            // Create tree of select part of query
            SelectNode selectNode = Parser.ParseSelectExpr(tokens);

            // Process parse tree and create list of variables to be printed
            SelectVisitor visitor = new SelectVisitor();
            selectNode.Accept(visitor);

            this.selectVariables = visitor.GetResult();
        }

        public List<SelectVariable> GetSelectVariables() => this.selectVariables;

        /// <summary>
        /// Checks correctness of given print expression.
        /// </summary>
        /// <param name="variableMap"> Map of variables </param>
        public void CheckCorrectnessOfSelect(VariableMap variableMap)
        {
            for (int i = 0; i < this.selectVariables.Count; i++)
            {
                if (this.selectVariables[i].name == "*") continue;

                if (variableMap.TryGetValue(this.selectVariables[i].name,
                                            out Tuple<int, Table> tuple))
                {
                    if (tuple.Item2 !=null && !tuple.Item2.ContainsProperty(this.selectVariables[i].propName))
                            throw new ArgumentException($"{this.GetType()}, select expression contains variable that is not defined");
                }
                else throw new ArgumentException($"{this.GetType()}, select expression contains variable that is not defined");
            }
        }
    }
    class SelectVariable
    {
        public string name { get; private set; }
        public string propName { get; private set; }

        public bool TrySetName(string n)
        {
            if (this.name == null) { this.name = n; return true; }
            else return false;
        }
        public bool TrySetPropName(string n)
        {
            if (this.propName == null) { this.propName = n; return true; }
            else return false;
        }
        public bool IsEmpty()
        {
            if ((this.name == null) && (this.propName == null)) return true;
            else return false;
        }
    }


}
