using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QueryEngine
{
    static class ExtensionArray
    {
        public static string ToString(this Element[] tmp)
        {
            string tmpString = "Thread: " + Thread.CurrentThread.ManagedThreadId.ToString() + " result: ";
            for (int i = 0; i < tmp.Length; i++)
            {
                tmpString +=  " " + tmp[i].ID.ToString();
            }
            
            return tmpString;
        }

        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }
    }

}
