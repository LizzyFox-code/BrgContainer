namespace Lod
{
    using System.Collections.Generic;
    using Unity.Mathematics;

    internal readonly struct LodComparer : IComparer<IndexLodPair>
    {
        public int Compare(IndexLodPair x, IndexLodPair y)
        {
            return (int)math.sign(x.LOD - y.LOD);
        }
    }
}