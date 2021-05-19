namespace QueryEngine
{

    /// <summary>
    /// Struct serves as a type for the key that is put into the dictionary when multigroup grouping is performed.
    /// </summary>
    internal readonly struct GroupDictKey
    {
        public readonly int hash;
        /// <summary>
        /// A position of the result row equivalent to the group representant.
        /// </summary>
        public readonly int position;

        public GroupDictKey(int hash, int position)
        {
            this.hash = hash;
            this.position = position;
        }


        public override int GetHashCode()
        {
            return hash;
        }

    }
}
