using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    interface ISorter
    {
        IResults Sort();
    }

    sealed class Sorter : ISorter
    {
        private IResults sortData;

        public Sorter(IResults sortData)
        {
            this.sortData = sortData;
        }

        public IResults Sort()
        {








            return this.sortData;
        }
    }
}
