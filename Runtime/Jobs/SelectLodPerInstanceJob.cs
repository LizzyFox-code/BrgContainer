﻿namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct SelectLodPerInstanceJob : IJobFilter
    {
        [ReadOnly]
        public NativeArray<PackedMatrix> ObjectToWorld;
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> LodPerInstance;

        public int DataOffset;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> Extents;
        public LODParams LODParams;
        public BatchLodDescription LodDescription;
        
        public bool Execute(int index)
        {
            var matrix = ObjectToWorld[index + DataOffset].fullMatrix;
            for (var i = 0; i < LodDescription.LodCount; i++)
            {
                GetPositionAndScale(matrix, i, out var position, out var worldScale);
                var distance = math.select(LODParams.DistanceScale * math.length(LODParams.CameraPosition - position),
                    LODParams.DistanceScale, LODParams.IsOrthographic);
                
                GetRelativeLodDistances(LodDescription, worldScale, out var lodDistances0, out var lodDistances1);
                var lodRange = LODRange.Create(lodDistances0, lodDistances1,1 << i);
                
                var lodIntersect = distance < lodRange.MaxDist && distance >= lodRange.MinDist;
                if (lodIntersect)
                {
                    LodPerInstance[index] = i;
                    return true;
                }
            }
            return false;
        }

        private void GetPositionAndScale(float4x4 matrix, int index, out float3 position, out float worldScale)
        {
            var aabb = new AABB
            {
                Center = float3.zero,
                Extents = Extents[index]
            };
            aabb = AABB.Transform(matrix, aabb);
            
            position = aabb.Center;
            
            var size = aabb.Size;
            worldScale = math.abs(size.x);
            worldScale = math.max(worldScale, math.abs(size.y));
            worldScale = math.max(worldScale, math.abs(size.z));
        }

        private static void GetRelativeLodDistances(BatchLodDescription lodDescription, float worldSpaceSize, out float4 lodDistances0, out float4 lodDistances1)
        {
            lodDistances0 = new float4(float.PositiveInfinity);
            lodDistances1 = new float4(float.PositiveInfinity);
            
            for (var i = 0; i < lodDescription.LodCount; ++i)
            {
                var d = worldSpaceSize / lodDescription[i];
                if (i < 4)
                    lodDistances0[i] = d;
                else
                    lodDistances1[i - 4] = d;
            }
        }
    }
}