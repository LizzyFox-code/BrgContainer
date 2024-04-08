namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
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
        [ReadOnly]
        public NativeArray<int> LodPerInstance;
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> InstanceCountPerLod;
        
        public int DataOffset;
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> Extents;
        
        public unsafe bool Execute(int index)
        {
            var matrix = ObjectToWorld[index + DataOffset];
            var lod = LodPerInstance[index];
            
            var aabb = new AABB
            {
                Center = float3.zero,
                Extents = Extents[lod]
            };
            aabb = AABB.Transform(matrix.fullMatrix, aabb);
 
            for (var i = 0; i < CullingPlanes.Length; i++)
            {
                var plane = CullingPlanes[i];
                var normal = plane.normal;
                var distance = math.dot(normal, aabb.Center) + plane.distance;
                var radius = math.dot(aabb.Extents, math.abs(normal));

                if (distance + radius <= 0)
                    return false;
            }

            Interlocked.Increment(ref UnsafeUtility.ArrayElementAsRef<int>(InstanceCountPerLod.GetUnsafePtr(), lod));
            return true;
        }
    }
}