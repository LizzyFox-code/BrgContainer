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
        public NativeArray<float> FadePerInstance;
        
        public NativeArray<int> VisibleIndices;
        
        public void Execute(int index)
        {
            var instanceIndex = VisibleIndices[index];
            var fadeValue = math.asint(FadePerInstance[instanceIndex]);

            instanceIndex &= 0x00FFFFFF;
            instanceIndex |= fadeValue << 24;

            VisibleIndices[index] = instanceIndex;
        }
    }
}