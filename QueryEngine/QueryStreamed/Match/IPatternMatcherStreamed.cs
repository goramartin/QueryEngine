using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine 
{
    /// <summary>
    /// The interface enahces the base interface for a method that will enable to pass
    /// result processor chain to matchers. This is done because the processor will be created 
    /// after the actual matchers.
    /// </summary>
    interface IPatternMatcherStreamed : IPatternMatcher
    {
        void PassResultProcessor(ResultProcessor resultProcessor);
    }
}
