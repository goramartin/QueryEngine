using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    interface IABTree<T>
    {
        int Count { get; }
        void Insert(T key);
    }
}
