namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct PopulateVisibleIndicesJob : IJobParallelForDefer
    {
        [ReadOnly]
        public NativeArray<int> VisibleIndicesReader;
        [WriteOnly]
        public NativeList<int>.ParallelWriter VisibleIndicesWriter;
        
        [ReadOnly]
        public NativeArray<LodFade> LodFadePerInstance;
        
        public BatchLodDescription LodDescription;
        
        public void Execute(int index)
        {
            var rawInstanceIndex = VisibleIndicesReader[index];
            var instanceIndex = rawInstanceIndex & 0x00FFFFFF;
            var instanceLodFade = LodFadePerInstance[instanceIndex];
            
            if(math.abs(instanceLodFade.Value - 1.0f) < float.Epsilon)
                return;

            var currentLod = rawInstanceIndex >> 24;
            var nextLod = currentLod + 1;
            if(nextLod >= LodDescription.LodCount)
                return;

            var nextInstanceIndex = instanceIndex & 0x00FFFFFF;
            nextInstanceIndex |= nextLod << 24;
            
            VisibleIndicesWriter.AddNoResize(nextInstanceIndex);
        }
    }
}