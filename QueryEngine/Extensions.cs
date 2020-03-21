using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    static class ExtensionArray
    {
        public static void Print(this Element[] tmp)
        {
            for (int i = 0; i < tmp.Length; i++)
            {
                Console.Write("{0} ", tmp[i].ID);
            }
            Console.WriteLine();
            Console.WriteLine(":");
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
