namespace QueryEngine
{
    /// <summary>
    /// Class is used as a holder for printing directives.
    /// When select initialises printing the class will serve as a base information for printing headers.
    /// Each reference class has this class as a way to override ToString() method.
    /// </summary>
    internal sealed class VariableReferenceNameHolder
    {
        /// <summary>
        /// Name of variable.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Property access to a variable.
        /// </summary>
        public string PropName { get; private set; }


        public VariableReferenceNameHolder(string name = null, string propName = null)
        {
            this.Name = name;
            this.PropName = propName;
        }

        /// <summary>
        /// Tries to set a name, will set if name is set to null.
        /// </summary>
        public bool TrySetName(string n)
        {
            if (this.Name == null) { this.Name = n; return true; }
            else return false;
        }
        /// <summary>
        /// Tries to set a property name, will set if property is set to null.
        /// </summary>
        public bool TrySetPropName(string n)
        {
            if (this.PropName == null) { this.PropName = n; return true; }
            else return false;
        }

        /// <summary>
        /// Check is the variable has no set contents.
        /// </summary>
        public bool IsEmpty()
        {
            if ((this.Name == null) && (this.PropName == null)) return true;
            else return false;
        }
    }


}
