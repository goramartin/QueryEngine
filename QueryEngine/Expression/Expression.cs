using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    abstract class ExpressionBaseNode
    {
        public bool IsNull {get; private set;}
        public abstract bool TryEvaluate();
        public abstract string GetValueAsString();
    }

    abstract class ExpressionReturnValueNode<T> : ExpressionBaseNode
    {





    }



}
