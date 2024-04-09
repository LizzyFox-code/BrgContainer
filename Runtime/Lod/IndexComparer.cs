namespace BrgContainer.Runtime.Lod
{
    using System.Collections.Generic;
    using Unity.Mathematics;

    internal readonly struct IndexComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            var lodX = x >> 24;
            var lodY = y >> 24;
            
            return (int)math.sign(lodX - lodY);
        }
    }
}