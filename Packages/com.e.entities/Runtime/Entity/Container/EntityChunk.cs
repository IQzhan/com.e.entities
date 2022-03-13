namespace E.Entities
{
    /// <summary>
    /// Entity chunk that contains entities.
    /// </summary>
    internal unsafe struct EntityChunk
    {
        public fixed byte data[EntityConst.ChunkSize];
    }
}