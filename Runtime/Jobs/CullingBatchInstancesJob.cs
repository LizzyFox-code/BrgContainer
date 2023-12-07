namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.CompilerServices;
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
            var objectToWorld = ObjectToWorld[index + DataOffset].fullMatrix;

            var boundingBoxMin = math.mul(objectToWorld, new float4(-Extents, 1.0f));
            var boundingBoxMax = math.mul(objectToWorld, new float4(Extents, 1.0f));

            return IsFrustumContainsBox(boundingBoxMin.xyz, boundingBoxMax.xyz);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsFrustumContainsBox(float3 min, float3 max)
        {
            float3 position;
            for (var i = 0; i < CullingPlanes.Length; i++)
            {
                var plane = CullingPlanes[i];
                position.x = plane.normal.x > 0 ? max.x : min.x;
                position.y = plane.normal.y > 0 ? max.y : min.y;
                position.z = plane.normal.z > 0 ? max.z : min.z;

                if (GetDistanceToPlane(plane.normal, plane.distance, position) < 0)
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetDistanceToPlane(float3 normal, float distance, float3 position)
        {
            return math.dot(normal, position) + distance;
        }
    }
}