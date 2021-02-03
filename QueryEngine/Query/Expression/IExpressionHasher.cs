namespace QueryEngine
{
    internal interface IExpressionHasher
    {
        int Hash(in TableResults.RowProxy row);
        
    }
}
