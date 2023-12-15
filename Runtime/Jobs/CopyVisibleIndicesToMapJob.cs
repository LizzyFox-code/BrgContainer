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
        public NativeArray<BatchInstanceIndices> VisibleIndicesPerBatch;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<int> VisibleIndices;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<int> VisibleCountPerChunk;
        
        public int BatchIndex;

        public unsafe void Execute()
        {
            if(VisibleIndices.Length == 0)
                return;
            
            var instanceIndices = new BatchInstanceIndices
            {
                Indices = (int*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * VisibleIndices.Length,
                    UnsafeUtility.AlignOf<int>(), Allocator.TempJob)
            };
            
            UnsafeUtility.MemCpy(instanceIndices.Indices, VisibleIndices.GetUnsafePtr(), VisibleIndices.Length * UnsafeUtility.SizeOf<int>());

            VisibleIndicesPerBatch[BatchIndex] = instanceIndices;
            VisibleCountPerChunk[BatchIndex] = VisibleIndices.Length;
        }
    }
}