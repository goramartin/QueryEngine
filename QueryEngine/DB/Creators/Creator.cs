
/*! \file
 
  Contains definition of creators from file.
  
  Creator is an interface for a class that creates a defined object in the template paramter.
  Creator makes use of a processor and optionaly some reader. Where processor build the entire objects.

 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
   
    /// <summary>
    /// Creator interface. Each creator object will create an object specified in a template.
    /// </summary>
    /// <typeparam name="T"> Value to be created. </typeparam>
    internal interface ICreator<T>
    {
        T Create();
    }

}
