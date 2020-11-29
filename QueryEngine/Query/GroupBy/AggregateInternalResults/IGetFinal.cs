using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QueryEngine 
{
    /// <summary>
    /// A function that will retreive results from the aggregate result storages.
    /// This interface will be shared among all storages. 
    /// </summary>
    /// <typeparam name="T">Return type of the aggregate funcs.</typeparam>
    interface IGetFinal<T>
    {
        T GetFinal(int position);
    }


}
