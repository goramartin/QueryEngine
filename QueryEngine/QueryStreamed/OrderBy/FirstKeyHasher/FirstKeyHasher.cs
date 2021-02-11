using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal abstract class FirstKeyHasher
    {
    }


    internal abstract class FirstKeyHasher<T> : FirstKeyHasher
    {
        public abstract int Hash(T value);
    }
}
