using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    readonly struct RowProxy
    {
        private readonly List<Element>[] elements;
        private readonly int index; 

        public RowProxy(List<Element>[] elements, int index)
        {
            this.elements = elements;
            this.index = index;
        }

        public Element this[int column]
        {
            get
            {
                if (column < 0 || column >= this.elements.Length)
                    throw new ArgumentOutOfRangeException($"{this.GetType()}, accessed column is out of range.");
                else return elements[column][this.index];
            }
        }

        public override string ToString()
        {
            string tmpString =  "Row: " + this.index + " result: ";
            for (int i = 0; i < this.elements.Length; i++)
                tmpString += " " + this[i].ID.ToString();
            
            return tmpString;
        }
    }
}
