namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct CopyVisibleIndicesToMapJob : IJob
    {
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchInstanceData> VisibleIndicesPerBatch;
        
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> VisibleIndices;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<int> VisibleCountPerChunk;
        
        public int BatchIndex;

        public unsafe void Execute()
        {
            if(VisibleIndices.Length == 0)
                return;
            
            var instanceIndices = new BatchInstanceData
            {
                Indices = (int*)VisibleIndices.GetUnsafePtr(), // no data copy, only ptr
            };
            
            instanceIndices.InstanceCountPerLod[]
            
            VisibleIndicesPerBatch[BatchIndex] = instanceIndices;
            VisibleCountPerChunk[BatchIndex] = VisibleIndices.Length;
        }
    }
}