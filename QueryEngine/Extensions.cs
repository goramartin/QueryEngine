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
        public static string Print(this Element[] tmp)
        {
            string tmpString = "Thread: " + Thread.CurrentThread.ManagedThreadId.ToString() + " result: ";
            for (int i = 0; i < tmp.Length; i++)
            {
                tmpString +=  " " + tmp[i].ID.ToString();
            }
            return tmpString;
        }

        public static List<Aggregate> CloneAggs(this List<Aggregate> aggs)
        {
            var tmp = new List<Aggregate>();

            for (int i = 0; i < aggs.Count; i++)
                tmp.Add(aggs[i].Clone());

            return tmp;
        }

        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        public static void AscPopulate(this int[] arr, int value)
        {
            for (int i = value; i < arr.Length; i++)
            {
                arr[i] = i;
            }
        }

    }

}
