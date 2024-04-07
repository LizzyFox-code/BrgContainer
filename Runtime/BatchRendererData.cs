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
        
        public readonly RendererDescription Description;
        public readonly float3 Extents;

        public readonly BatchLodDescription BatchLodDescription;

        public ref BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_BatchLodRendererData4[index];
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