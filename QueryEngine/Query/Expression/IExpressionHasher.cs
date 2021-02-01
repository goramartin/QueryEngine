using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal interface IExpressionHasher
    {
        int Hash(in TableResults.RowProxy row);
        
    }
}
