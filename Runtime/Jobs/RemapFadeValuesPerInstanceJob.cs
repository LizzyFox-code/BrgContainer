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

            var fadeValue = (int)math.select(PackFloatToUint8(-rawLodFade.Value), PackFloatToUint8(rawLodFade.Value),
                rawLodFade.Lod == lod);
            fadeValue -= 127;
            
            instanceIndex |= fadeValue << 24;
            VisibleIndices[index] = instanceIndex;
        }
        
        // float [-1.0f... 1.0f] -> uint [0...254]
        private static uint PackFloatToUint8(float percent)
        {
            var packed = (uint)((1.0f + percent) * 127.0f + 0.5f);
            // avoid zero
            if (percent < 0.0f)
                packed = math.clamp(packed, 0, 126);
            else
                packed = math.clamp(packed, 128, 254);
            return packed;
        }
    }
}