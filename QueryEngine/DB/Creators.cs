
/*! \file
 
  Contains definition of creators from file.
  Takes a file and creates given type from the file.
  Reading and building of the type is done in states.
  The class creator takes reader (reads given file) and processor.
  Processor is given words from a file and builds the class.
  When reading is finished the processor is halted and the creator can
  return desired object.
  
   There are three creators, for tables (same for edges and node tables),
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
    /// State of creators.
    /// Each creator has its own specialised states.
    /// </summary>
    interface IProcessorState<T>
    {
        void Process(IProcessor<T> processor, string param);
    }
    /// <summary>
    /// Interface for state oriented processing of incoming files.
    /// </summary>
    interface IProcessor<T>
    {
        void SetNewState(IProcessorState<T> state);
        bool Finished();
        void Process(string param);
        T GetResult();

        void PassParameters(params object[] prms);    
    }
    
    interface ICreator<T>
    {
        T Create();
    }

    /// <summary>
    /// Class takes reader and processor that proccesses words from reader.
    /// Main purpose is to have a general way to create classes from files.
    /// </summary>
    class CreatorFromFile<T> : ICreator<T>
    {
        IReader reader;
        IProcessor<T> processor;

        public CreatorFromFile(IReader reader, IProcessor<T> processor)
        {
            this.reader = reader;
            this.processor = processor;
        }

        /// <summary>
        /// Processes file. Reading until reached end point of a processor.
        /// </summary>
        /// <returns> Value based on template. </returns>
        public T Create()
        {
            while (!this.processor.Finished())
            {
                string wd = this.reader.Read();
                this.processor.Process(wd);

            }
            this.reader.Dispose();
            return this.processor.GetResult();
        }

    }
}
