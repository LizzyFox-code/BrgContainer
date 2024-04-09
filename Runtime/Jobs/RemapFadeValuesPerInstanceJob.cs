namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct RemapFadeValuesPerInstanceJob : IJobParallelForDefer
    {
        [ReadOnly]
        public NativeArray<LodFade> FadePerInstanceReader;
        
        public NativeArray<int> VisibleIndices;
        
        public void Execute(int index)
        {
            var rawInstanceIndex = VisibleIndices[index];
            var instanceIndex = rawInstanceIndex & 0x00FFFFFF;
            var lod = rawInstanceIndex >> 24;
            var rawLodFade = FadePerInstanceReader[instanceIndex];
            
            var fadeValue = (int)math.round(math.select(1.0f - rawLodFade.Value, rawLodFade.Value, rawLodFade.Lod == lod) * 255);
            fadeValue = math.max(0, math.min(255, fadeValue));
            
            instanceIndex |= fadeValue << 24;

            VisibleIndices[index] = instanceIndex;
        }
    }
}