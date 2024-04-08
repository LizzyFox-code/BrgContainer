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
    internal struct SelectLodPerInstanceJob : IJobFilter
    {
        [ReadOnly]
        public NativeArray<PackedMatrix> ObjectToWorld;
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> LodPerInstance;

        public int DataOffset;
        
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
                
                var relativeDistances = GetRelativeLodDistances(LodDescription, worldScale);
                var lodRange = LODRange.Create(relativeDistances, 1 << i);
                
                var lodIntersect = distance < lodRange.MaxDist && distance >= lodRange.MinDist;
                if (lodIntersect)
                {
                    LodPerInstance[index] = i;
                    return true;
                }
            }
            return true;
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

        private static float4 GetRelativeLodDistances(BatchLodDescription lodDescription, float worldSpaceSize)
        {
            var lodDistances = new float4(float.PositiveInfinity);
            var lodGroupLODs = lodDescription.LodDistances;
            for (var i = 0; i < lodDescription.LodCount; ++i)
            {
                var d = worldSpaceSize / lodGroupLODs[i];
                lodDistances[i] = d;
            }

            return lodDistances;
        }
    }
}