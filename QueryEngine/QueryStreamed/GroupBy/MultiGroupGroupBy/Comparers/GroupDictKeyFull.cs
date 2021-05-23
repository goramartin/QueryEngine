namespace QueryEngine
{
    /// <summary>
    /// A class representing a Dictionary key in the streamed version of the grouping.
    /// The only difference between the class GroupDictKey is that it contains the full information about the
    /// row and not just a position indicator.
    /// </summary>
    internal readonly struct GroupDictKeyFull
    {
        public readonly int hash;
        public readonly TableResults.RowProxy row;

        public GroupDictKeyFull(int hash, TableResults.RowProxy row)
        {
            this.hash = hash;
            this.row = row;
        }

        public override int GetHashCode()
        {
            return hash;
        }
    }
}
