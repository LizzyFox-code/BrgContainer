namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BatchRendererData
    {
        private readonly FixedBatchLodRendererData4 m_BatchLodRendererData4;
        
        public readonly uint SubMeshIndex;
        public readonly RendererDescription Description;
        public readonly float3 Extents;

        public ref BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_BatchLodRendererData4[index];
        }

        public BatchRendererData(uint subMeshIndex, float3 extents, in RendererDescription description)
        {
            m_BatchLodRendererData4 = default;

            SubMeshIndex = subMeshIndex;
            Extents = extents;
            Description = description;
        }
    }
}