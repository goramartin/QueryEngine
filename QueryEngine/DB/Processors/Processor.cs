
/*! \file
  
   Contains base class for processors. Processors gets input parameters and creates
   object defined in a template paramter. Used by a creator class.

   Each processor imlements state pattern.
   
   There are three processors, for tables (same for edges and node tables),
   one that creates vertices from a data file and the third that creates list of 
   edges (in/out).
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// State of processor.
    /// Each processor has its own specialised states.
    /// </summary>
    interface IProcessorState<T>
    {
        void Process(IProcessor<T> processor, string param);
    }
    /// <summary>
    /// Interface for state oriented processing of a file.
    /// </summary>
    /// <typeparam name="T"> Value to be created. </typeparam>
    interface IProcessor<T>
    {
        void SetNewState(IProcessorState<T> state);
        bool Finished();
        void Process(string param);
        T GetResult();

        void PassParameters(params object[] prms);
    }

}
