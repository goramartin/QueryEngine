/*! \file
Contains a base class for processors. 
Processors get input parameters and create objects defined in a template parameter. 
Processors are used inside of a Creator class.

Each processor implements the State pattern.

There are three processors, one for loading definitions of tables (same for edges and node tables),
one that creates a List of vertices from a data file and the third that creates a List of edges (in/out) from a data file.
The processor states are singletons and flyweight, they are available for the entire run of the program, in case the program wants to read more definitions.
 */

namespace QueryEngine
{
    /// <summary>
    /// The state of processor.
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
    /// An interface for a state oriented processing of a file.
    /// </summary>
    /// <typeparam name="T"> An object/struct/value to be created. </typeparam>
    internal interface IProcessor<T>
    {
        void SetNewState(IProcessorState<T> state);
        bool Finished();
        void Process(string param);
        T GetResult();
        void PassParameters(params object[] prms);
    }

}
