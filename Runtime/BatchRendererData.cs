namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchRendererData : INativeDisposable
    {
        [NativeDisableContainerSafetyRestriction]
        private UnsafeList<float3> m_Extents;
        
        private FixedBatchLodRendererData4 m_BatchLodRendererData4;
        
        public readonly RendererDescription Description;
        public readonly BatchLodDescription BatchLodDescription;

        public unsafe NativeArray<float3> Extents
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CollectionHelper.ConvertExistingDataToNativeArray<float3>(m_Extents.Ptr, m_Extents.Length,
                m_Extents.Allocator, false);
        }

        public BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => m_BatchLodRendererData4[index];
            set => m_BatchLodRendererData4[index] = value;
        }

        public BatchRendererData(ref UnsafeList<float3> extents, in RendererDescription description, in BatchLodDescription batchLodDescription)
        {
            m_BatchLodRendererData4 = default;
            
            m_Extents = extents;
            Description = description;

            BatchLodDescription = batchLodDescription;
        }

        public void Dispose()
        {
            if(m_Extents.IsCreated)
                m_Extents.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!m_Extents.IsCreated)
                return inputDeps;
            
            return m_Extents.Dispose(inputDeps);
        }
    }
}