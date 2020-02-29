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

        // to do
        public void CheckCorrectnessOfSelect(Graph graph, VariableMap variableMap)
        {






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
