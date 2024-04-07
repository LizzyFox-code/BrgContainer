namespace BrgContainer.Runtime.Lod
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    internal readonly struct IndexComparer : IComparer<int>
    {
        [NativeDisableContainerSafetyRestriction]
        private readonly NativeArray<int> m_LODS;

        public IndexComparer(NativeArray<int> lods)
        {
            m_LODS = lods;
        }
        
        public int Compare(int x, int y)
        {
            var lodX = m_LODS[x];
            var lodY = m_LODS[y];
            
            return (int)math.sign(lodX - lodY);
        }
    }
}