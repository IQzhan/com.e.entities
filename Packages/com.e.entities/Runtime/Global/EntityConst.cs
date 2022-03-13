namespace E.Entities
{
    internal static class EntityConst
    {
        /// <summary>
        /// Max ComponentType count.
        /// </summary>
        public const int MaxComponentTypeCount = 256;

        /// <summary>
        /// 
        /// </summary>
        public const int MaxComponentTypeCountRemMask = 0b11111111;

        /// <summary>
        /// Max EntityScene count.
        /// </summary>
        public const int MaxSceneCount = 32;

        /// <summary>
        /// Max EntityGroup count each EntityScene.
        /// </summary>
        public const int MaxEntityGroupCountEachScene = 512;

        /// <summary>
        /// Max entity count each EntityGroup, 8388607
        /// </summary>
        public const int MaxEntityCountEachGroup = 0x7FFFFF;

        /// <summary>
        /// Chunk size, 16KByte
        /// </summary>
        public const int ChunkSize = 1 << 14;

        /// <summary>
        /// Expand size while chunkList full.
        /// </summary>
        public const int ChunkListExpandSize = 32;

        /// <summary>
        /// 
        /// </summary>
        public const int BitMaskExpandSize = 1 << 10;
    }
}