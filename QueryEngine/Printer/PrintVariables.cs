using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Represents a holder of information about one column from a select expression.
    /// </summary>
    abstract class PrinterVariable
    {
        /// <summary>
        /// Index of a variable in array (result) that is printed during select at this print variable.
        /// </summary>
        public int VariableIndex { get; protected set; }

        /// <summary>
        /// String representation of given select request.
        /// A need to print a header of label.
        /// </summary>
        public SelectVariable selectVariable { get; protected set; }

        public PrinterVariable(SelectVariable selectVariable)
        {
            this.selectVariable = selectVariable;
        }

        /// <summary>
        /// Creates a header representing  this print variable.
        /// </summary>
        /// <returns> Header as a string. </returns>
        public abstract string GetHeader();

        /// <summary>
        /// Collects string values of given element and returns them in one concatenated string.
        /// If the variable represents name.prop then it returns only one property as string otherwise
        /// it will create a concatenation of type and id of an element of all properties on the given element.
        /// </summary>
        /// <param name="element"> Element whose properties will be printed.</param>
        /// <returns> String representation of properties. </returns>
        public abstract string GetSelectVariableAsString(Element element);


        /// <summary>
        /// Factory for printer variables. Based on given select variable it decides 
        /// which variable to make.
        /// </summary>
        /// <param name="selectVariable"> Select variable containing information about printing.</param>
        /// <param name="map"> Map containing all variables. </param>
        /// <returns> Printer variable. </returns>
        public static PrinterVariable PrinterVariableFactory(SelectVariable selectVariable, VariableMap map)
        {
            if (selectVariable.propName != null)
                return new PrinterVariableProperty(selectVariable, map);
            else return new PrinterVariableID(selectVariable, map);
        }
    }

    /// <summary>
    /// Class representing select expression var.prop.
    /// This class wil return string representation of a stored property.
    /// </summary>
    class PrinterVariableProperty : PrinterVariable
    {

        /// <summary>
        /// Creates a property print variable.
        /// Check if the variable is defined.
        /// </summary>
        /// <param name="selectVariable"> Select variable containing information about printing.</param>
        /// <param name="map"> Map containing all variables. </param>
        /// <returns> Printer variable. </returns>
        public PrinterVariableProperty(SelectVariable selectVariable, VariableMap map) : base(selectVariable)
        {
            if (!map.TryGetValue(selectVariable.name, out Tuple<int, Table> tuple))
                throw new ArgumentException($"{this.GetType()}, variable name does not exist. Name = {selectVariable.name}.");

            this.VariableIndex = tuple.Item1;
        }

        /// <summary>
        /// Returns property on given index on a given element as a string.
        /// </summary>
        /// <param name="element"> Graph element.</param>
        public override string GetSelectVariableAsString(Element element)
        {
            return element.Table.TryGetElementValueAsString(element.ID, this.selectVariable.propName);
        }

        /// <summary>
        /// Returns name of column in format variable.property
        /// </summary>
        public override string GetHeader()
        {
            if (this.selectVariable.label != null) return this.selectVariable.label;
            else return this.selectVariable.name + "." + this.selectVariable.propName;
        }
    }

    /// <summary>
    /// Class representing select expression var.
    /// Gives concatenation of type of variable together with its id.
    /// </summary>
    class PrinterVariableID : PrinterVariable
    {

        /// <summary>
        /// Creates a id print variable.
        /// Check if the variable is defined.
        /// </summary>
        /// <param name="selectVariable"> Select variable containing information about printing.</param>
        /// <param name="map"> Map containing all variables. </param>
        /// <returns> Printer variable. </returns>
        public PrinterVariableID(SelectVariable selectVariable, VariableMap map) : base(selectVariable)
        {
            if (!map.TryGetValue(selectVariable.name, out Tuple<int, Table> tuple))
                throw new ArgumentException($"{this.GetType()}, variable name does not exist. Name = {selectVariable.name}.");

            this.VariableIndex = tuple.Item1;
        }

        /// <summary>
        /// Returns name of column.
        /// </summary>
        public override string GetHeader()
        {
            return this.selectVariable.name;
        }

        /// <summary>
        /// Returns ID of an element together with type of variable.
        /// </summary>
        /// <param name="element"> Graph element. </param>
        /// <returns> String concatenation of the elements properties. </returns>
        public override string GetSelectVariableAsString(Element element)
        {
            return element.ID.ToString();
        }
    }
}
