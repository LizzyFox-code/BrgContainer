namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct CopyVisibleIndicesToMapJob : IJob
    {
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchInstanceData> InstanceDataPerBatch;
        [ReadOnly]
        public NativeArray<int> InstanceCountPerLod;
        
        [ReadOnly]
        public NativeArray<int> VisibleIndices;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<int> VisibleCountPerChunk;
        
        public int BatchIndex;

        public unsafe void Execute()
        {
            if(VisibleIndices.Length == 0)
                return;
            
            BatchInstanceData instanceIndices = default;
            instanceIndices.Indices = (int*)UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<int>() * VisibleIndices.Length,
                UnsafeUtility.AlignOf<int>(), Allocator.TempJob, 0);
            UnsafeUtility.MemCpy(instanceIndices.Indices, VisibleIndices.GetUnsafeReadOnlyPtr(), UnsafeUtility.SizeOf<int>() * VisibleIndices.Length);

            for (var i = 0; i < FixedBatchLodRendererData4.Count; i++)
            {
                instanceIndices.InstanceCountPerLod[i] = InstanceCountPerLod[i];
            }
            
            InstanceDataPerBatch[BatchIndex] = instanceIndices;
            VisibleCountPerChunk[BatchIndex] = VisibleIndices.Length;
        }
    }
}