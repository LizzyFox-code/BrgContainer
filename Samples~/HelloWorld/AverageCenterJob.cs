namespace Samples.Hello_World
{
    using System.Runtime.InteropServices;
    using BrgContainer.Runtime;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    public struct AverageCenterJob : IJob 
    {
        [ReadOnly]
        public BatchInstanceDataBuffer InstanceDataBuffer;

        [NativeDisableUnsafePtrRestriction]
        public NativeReference<float3> Center;

        public int Size;
        public int ObjectToWorldPropertyId;

        public void Execute() 
        {
            var center = float3.zero;
            for (var i = 0; i < Size; i++)
            {
                var m = InstanceDataBuffer.ReadInstanceData<PackedMatrix>(i, ObjectToWorldPropertyId);
                center += m.GetPosition();
            }

            Center.Value = center / Size;
        }
    }
}