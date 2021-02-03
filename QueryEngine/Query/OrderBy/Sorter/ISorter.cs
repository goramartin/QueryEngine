namespace QueryEngine
{
    /// <summary>
    /// Class that will sort table results.
    /// </summary>
    internal abstract class ISorter
    {
        public abstract ITableResults Sort();
    }
}
