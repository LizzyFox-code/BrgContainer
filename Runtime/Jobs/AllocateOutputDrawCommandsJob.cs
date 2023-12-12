namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct AllocateOutputDrawCommandsJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe BatchCullingOutputDrawCommands* OutputDrawCommands;
        [ReadOnly]
        public NativeArray<int> Counters;
        
        public unsafe void Execute()
        {
            var visibleCount = Counters[0];
            var drawRangesCount = Counters[1];
            var drawCommandCount = Counters[2];

            OutputDrawCommands->visibleInstanceCount = visibleCount;
            OutputDrawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * visibleCount,
                UnsafeUtility.AlignOf<int>(), Allocator.TempJob);
            
            OutputDrawCommands->drawRangeCount = drawRangesCount;
            OutputDrawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>() * drawRangesCount,
                UnsafeUtility.AlignOf<BatchDrawRange>(), Allocator.TempJob);

            OutputDrawCommands->drawCommandCount = drawCommandCount;
            OutputDrawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>() * drawCommandCount,
                UnsafeUtility.AlignOf<BatchDrawCommand>(), Allocator.TempJob);
        }
    }
}