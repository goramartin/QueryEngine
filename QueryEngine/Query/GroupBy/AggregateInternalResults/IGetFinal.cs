namespace QueryEngine 
{
    /// <summary>
    /// A function that will retrieve results from the aggregate result storage.
    /// This interface will be shared among all storages. 
    /// </summary>
    /// <typeparam name="T"> A return type of the aggregate function.</typeparam>
    interface IGetFinal<T>
    {
        T GetFinal(int position);
    }


}
