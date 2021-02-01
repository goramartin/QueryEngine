/*! \file
Contains base class for processors. Processors gets input parameters and create
objects defined in a template parameter. Processors are used inside of a Creator class.
Each processor implements state pattern.
There are three processors, one for loading definitions of tables (same for edges and node tables),
one that creates a list of vertices from a data file and the third that creates a list of 
edges (in/out) from a data file.
The processor states are singletons and flyweight, they are available for the entire run of the program.
In case the program wants to read more definitions.
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
    internal interface IProcessorState<T>
    {
        /// <summary>
        /// Processes a given parameter. 
        /// </summary>
        /// <param name="processor"> A parsed parameter from a file. </param>
        /// <param name="param"> A processor to be used for processing the parameter. </param>
        void Process(IProcessor<T> processor, string param);
    }
    /// <summary>
    /// Interface for a state oriented processing of a file.
    /// </summary>
    /// <typeparam name="T"> Value to be created. </typeparam>
    internal interface IProcessor<T>
    {
        void SetNewState(IProcessorState<T> state);
        bool Finished();
        void Process(string param);
        T GetResult();

        void PassParameters(params object[] prms);
    }

}
