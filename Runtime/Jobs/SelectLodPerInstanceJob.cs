namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using System.Threading;
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
        [ReadOnly]
        public NativeArray<int> Indices;
        [WriteOnly]
        public NativeArray<int> LodPerInstance;
        [WriteOnly]
        public NativeArray<int> InstanceCountPerLod;

        public float4x4 ViewerObjectToWorld;
        public BatchLodDescription LodDescription;
        
        public unsafe bool Execute(int index)
        {
            var instanceIndex = Indices[index];
            var matrix = ObjectToWorld[instanceIndex];

            var aPosition = matrix.GetPosition();
            var bPosition = ViewerObjectToWorld.c3.xyz;

            var distance = math.distance(aPosition, bPosition);

            var lod = 0;
            for (var i = FixedBatchLodRendererData4.Count - 1; i >= 0 ; i--)
            {
                var lodDistance = LodDescription[i];
                var isGreaterOrEqual = distance >= lodDistance;
                lod = math.select(lod, i, isGreaterOrEqual);
            }

            if (lod == FixedBatchLodRendererData4.Count - 1)
                return false; // culled
            
            Interlocked.Increment(ref UnsafeUtility.ArrayElementAsRef<int>(InstanceCountPerLod.GetUnsafePtr(), lod));
            LodPerInstance[instanceIndex] = lod;
            
            return true;
        }
    }
}