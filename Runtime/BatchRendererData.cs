namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchRendererData
    {
        private FixedBatchLodRendererData4 m_BatchLodRendererData4;
        
        public readonly RendererDescription Description;
        public readonly float3 Extents;

        public readonly BatchLodDescription BatchLodDescription;

        public BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => m_BatchLodRendererData4[index];
            set => m_BatchLodRendererData4[index] = value;
        }

        public BatchRendererData(float3 extents, in RendererDescription description, in BatchLodDescription batchLodDescription)
        {
            m_BatchLodRendererData4 = default;
            
            Extents = extents;
            Description = description;

            BatchLodDescription = batchLodDescription;
        }
    }
}