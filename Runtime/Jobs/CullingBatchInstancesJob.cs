namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct CullingBatchInstancesJob : IJobFilter
    {
        [ReadOnly]
        public NativeArray<Plane> CullingPlanes;
        [ReadOnly]
        public NativeArray<PackedMatrix> ObjectToWorld;

        public int DataOffset;
        public float3 Extents;
        
        public bool Execute(int index)
        {
            var position = ObjectToWorld[index + DataOffset].GetPosition();
            for (var i = 0; i < CullingPlanes.Length; i++)
            {
                var normal = CullingPlanes[i].normal;
                var distance = CullingPlanes[i].distance;

                if (math.dot(Extents, math.abs(normal)) + math.dot(normal, position) + distance <= 0.0f)
                    return false;
            }

            return true;
        }
    }
}