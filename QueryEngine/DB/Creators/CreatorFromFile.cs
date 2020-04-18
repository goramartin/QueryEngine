﻿/*! \file
 
  Contains definition of creators from file.
  Takes a file and creates given type from the file.
  Reading and building of the type is done in states.
  The class creator takes reader (reads given file) and processor (forms the wanted object).
  Processor is given words from a file and builds the class.
  When reading is finished the processor is halted and the creator can
  return desired object.
   
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
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