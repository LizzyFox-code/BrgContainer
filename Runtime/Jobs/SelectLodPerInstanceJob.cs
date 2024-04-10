namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct SelectLodPerInstanceJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<PackedMatrix> ObjectToWorld;

        [WriteOnly]
        public NativeList<int>.ParallelWriter VisibleIndicesWriter;
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<LodFade> LodFadePerInstance;

        public int DataOffset;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> Extents;
        public LODParams LODParams;
        public BatchLodDescription LodDescription;
        
        public unsafe void Execute(int index)
        {
            var matrix = ObjectToWorld[index + DataOffset].fullMatrix;
            for (var i = 0; i < LodDescription.LodCount; i++)
            {
                GetPositionAndScale(matrix, i, out var position, out var worldScale);
                var distance = math.select(LODParams.DistanceScale * math.length(LODParams.CameraPosition - position), LODParams.DistanceScale, LODParams.IsOrthographic);
                
                GetRelativeLodDistances(LodDescription, worldScale, out var lodDistances0, out var lodDistances1);
                var lodRange = LODRange.Create(lodDistances0, lodDistances1,1 << i);
                if (distance < lodRange.MaxDistance && distance >= lodRange.MinDistance)
                {
                    var fadeValue = 1.0f;
                    if(LodDescription.FadeMode == LODFadeMode.CrossFade)
                    {
                        var diff = math.max(0.0f, lodRange.MaxDistance - distance);
                        var fadeDistance = math.max(0.0f, lodRange.MaxDistance - lodRange.MinDistance) * LodDescription.FadeWidth[i];
                        if (diff < fadeDistance)
                        {
                            fadeValue = math.saturate(math.sin(diff / fadeDistance * math.PI * 0.5f));
                        }
                    }

                    AddInstances(index, i, fadeValue);
                    return;
                }
            }
        }

        private void AddInstances(int index, int lod, float fadeValue)
        {
            LodFadePerInstance[index] = new LodFade
            {
                Value = fadeValue,
                Lod = lod
            };
            
            var instanceIndex = index & 0x00FFFFFF;
            instanceIndex |= lod << 24;
            VisibleIndicesWriter.AddNoResize(instanceIndex);
                    
            if(math.abs(fadeValue - 1.0f) < 0.01f)
                return;
                    
            var nextLod = lod + 1;
            if(nextLod >= LodDescription.LodCount)
                return;
                    
            var nextInstanceIndex = index & 0x00FFFFFF;
            nextInstanceIndex |= nextLod << 24;
            VisibleIndicesWriter.AddNoResize(nextInstanceIndex);
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

        private static unsafe void GetRelativeLodDistances(BatchLodDescription lodDescription, float worldSpaceSize, out float4 lodDistances0, out float4 lodDistances1)
        {
            lodDistances0 = new float4(float.PositiveInfinity);
            lodDistances1 = new float4(float.PositiveInfinity);
            
            for (var i = 0; i < lodDescription.LodCount; ++i)
            {
                var d = worldSpaceSize / lodDescription.LodDistances[i];
                if (i < 4)
                    lodDistances0[i] = d;
                else
                    lodDistances1[i - 4] = d;
            }
        }
    }
}