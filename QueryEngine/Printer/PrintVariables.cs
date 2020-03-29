using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Represents a holder of information about one select column.
    /// </summary>
    abstract class PrinterVariable
    {
        /// <summary>
        /// Index of a variable that is printed during select at this print variable.
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
        /// it will create a concatenation of values of all properties on the given element.
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
            else return new PrinterVariableEveryProperty(selectVariable, map);
        }
    }

    /// <summary>
    /// Class representing select expression var.prop.
    /// This class wil return string representation of a stored property.
    /// </summary>
    class PrinterVariableProperty : PrinterVariable
    {
        /// <summary>
        /// Index of a property that will be accessed during string request.
        /// </summary>
        public int PropertyIndex { get; protected set; }

        public PrinterVariableProperty(SelectVariable selectVariable, VariableMap map) : base(selectVariable)
        {
            if (!map.TryGetValue(selectVariable.name, out Tuple<int, Table> tuple))
                throw new ArgumentException($"{this.GetType()}, variable name does not exist. Name = {selectVariable.name}.");

            this.VariableIndex = tuple.Item1;
            this.PropertyIndex = tuple.Item2.GetIndexOfProperty(selectVariable.propName);
            
            if (this.PropertyIndex == -1)
                throw new ArgumentException($"{this.GetType()}, property name does not exist. Name = {selectVariable.propName}.");
        }

        /// <summary>
        /// Returns property on given index on a given element as a string.
        /// </summary>
        /// <param name="element"> Graph element.</param>
        public override string GetSelectVariableAsString(Element element)
        {
            return element.Table.GetValueAsString(element.ID, this.PropertyIndex);
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
    /// Gives a concatenation of all property values for a given element.
    /// </summary>
    class PrinterVariableEveryProperty : PrinterVariable
    {
        StringBuilder stringBuilder;


        public PrinterVariableEveryProperty(SelectVariable selectVariable, VariableMap map) : base(selectVariable)
        {
            this.stringBuilder = new StringBuilder();
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
        /// Concatenates property values into one string of given element.
        /// </summary>
        /// <param name="element"> Graph element. </param>
        /// <returns> String concatenation of the elements properties. </returns>
        public override string GetSelectVariableAsString(Element element)
        {
            this.stringBuilder.Clear();

            int propCount = element.Table.Properties.Count;
            for (int i = 0; i < propCount ; i++)
            {
                this.stringBuilder.Append(element.Table.GetValueAsString(element.ID, i));
                if (i+1 != propCount) this.stringBuilder.Append(' ');
            }
            return stringBuilder.ToString();
        }
    }
}
