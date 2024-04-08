namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchRendererData : INativeDisposable
    {
        private FixedBatchLodRendererData4 m_BatchLodRendererData4;
        
        public readonly RendererDescription Description;
        public readonly BatchLodDescription BatchLodDescription;

        public NativeArray<float3> Extents;

        public BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => m_BatchLodRendererData4[index];
            set => m_BatchLodRendererData4[index] = value;
        }

        public BatchRendererData(NativeArray<float3> extents, in RendererDescription description, in BatchLodDescription batchLodDescription)
        {
            m_BatchLodRendererData4 = default;
            
            Extents = extents;
            Description = description;

            BatchLodDescription = batchLodDescription;
        }

        public void Dispose()
        {
            Extents.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return Extents.Dispose(inputDeps);
        }
    }
}