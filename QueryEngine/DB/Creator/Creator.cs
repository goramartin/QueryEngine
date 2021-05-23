/*! \file
Contains generic definition of an interface for creators.
A creator is an interface for a class that creates an object defined
in the template parameter.
Creators make use of a processor and optionally a reader from a file. 
Creators are used during creation of a database. 
They load data of vertices and edges into dictionaries, also they load the vertices and
edges into the database Lists.
 */

namespace QueryEngine
{
   
    /// <summary>
    /// Creator interface. 
    /// Each creator object will create an object specified in a template.
    /// </summary>
    /// <typeparam name="T"> A struct/class to be created. </typeparam>
    internal interface ICreator<T>
    {
        T Create();
    }

}
