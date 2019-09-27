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
                Console.Write("{0} ", tmp[i].GetID());
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

    static class ExtensionPattern
    {
        public static BaseMatch GetMatch(this List<List<BaseMatch>> pattern, int position)
        {
            int p = 0;
            for (int i = 0; i < pattern.Count; i++)
            {
                for (int k = 0; k < pattern[i].Count; k++)
                {
                    if (p == position) return pattern[i][k];
                    p++;
                }
            }
            throw new ArgumentException("QueryCheck, could not find any variable.");
        }

        public static int GetCount(this List<List<BaseMatch>> pattern)
        {
            int c = 0;
            for (int i = 0; i < pattern.Count; i++)
            {
                c += pattern[i].Count;
            }
            return c;
        }

    }

}
