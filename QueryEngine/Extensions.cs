using System.Threading;

namespace QueryEngine
{
    static class ExtensionArray
    {
        public static string Print(this Element[] tmp)
        {
            string tmpString = "Thread: " + Thread.CurrentThread.ManagedThreadId.ToString() + " result: ";
            for (int i = 0; i < tmp.Length; i++)
                tmpString +=  " " + tmp[i].ID.ToString();
            return tmpString;
        }

        /// <summary>
        /// Populates array with the given value.
        /// </summary>
        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
                arr[i] = value;
        }

        /// <summary>
        /// Populate array with increasing values of the given value.
        /// </summary>
        public static void AscPopulate(this int[] arr, int value)
        {
            for (int i = value; i < arr.Length; i++)
                arr[i] = i;
        }

    }

}
