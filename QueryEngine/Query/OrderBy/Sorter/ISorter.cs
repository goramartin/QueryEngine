namespace QueryEngine
{
    /// <summary>
    /// An interface that provides sorting method for ITableResults interface.
    /// </summary>
    internal abstract class ISorter
    {
        public abstract ITableResults Sort();
    }
}
