/*! \file
Contains definition of creators from file. 
Creators from a file take a file and create a given type (template parameter) from the file.
Reading and building of the type is done in states. 
The class creator takes reader (reads given file) and processor (forms the wanted object).
Processor is given words from a file and builds the class.
When reading is finished the processor is halted and the
creator can return desired object.
*/

namespace QueryEngine
{
    /// <summary>
    /// Class takes reader and processor that proccesses words from reader.
    /// Main purpose is to have a general way to create objects from files.
    /// An instance is unusable after a processor has finished.
    /// </summary>
    internal sealed class CreatorFromFile<T> : ICreator<T>
    {
        private IReader reader;
        private IProcessor<T> processor;

        /// <param name="reader"> A stream reader that returns words from a file. </param>
        /// <param name="processor"> A processor that creates the templated type. </param>
        public CreatorFromFile(IReader reader, IProcessor<T> processor)
        {
            this.reader = reader;
            this.processor = processor;
        }

        /// <summary>
        /// Processes file. 
        /// Reading until reached end point of a processor.
        /// It assumes that the reader will not fail until the processor finishes.
        /// </summary>
        /// <returns> A value based on the template. </returns>
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
