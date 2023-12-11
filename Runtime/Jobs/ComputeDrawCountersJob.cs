namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct ComputeDrawCountersJob : IJobFor
    {
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> DrawCounters; // 0 - is visible count, 1 - is draw ranges count, 2 - is draw command count
        [NativeDisableParallelForRestriction]
        public NativeArray<int> VisibleCountPerBatch;
        [NativeDisableParallelForRestriction]
        public NativeArray<BatchGroupDrawRange> DrawRangesData;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchGroup> BatchGroups;

        public unsafe void Execute(int index)
        {
            var batchGroup = BatchGroups[index];
            var subBatchCount = batchGroup.GetWindowCount();

            var validSubBatchCount = 0;
            var visibleCountPerBatchGroup = 0;
            for (var i = 0; i < subBatchCount; i++)
            {
                var visibleCountPerBatch = VisibleCountPerBatch[index + i];
                if(visibleCountPerBatch == 0) // there is no any visible instances for this batch
                    continue;

                visibleCountPerBatchGroup += visibleCountPerBatch;
                validSubBatchCount++;
            }
            
            ref var drawRangeDataRef = ref UnsafeUtility.ArrayElementAsRef<BatchGroupDrawRange>(DrawRangesData.GetUnsafePtr(), index);
            if(validSubBatchCount == 0)
            {
                for (var i = index + 1; i < BatchGroups.Length; i++)
                {
                    ref var nextDrawRangeDataRef = ref UnsafeUtility.ArrayElementAsRef<BatchGroupDrawRange>(DrawRangesData.GetUnsafePtr(), i);
                    Interlocked.Decrement(ref nextDrawRangeDataRef.IndexOffset);
                }
                return;
            }
            
            ref var visibleCountRef = ref UnsafeUtility.ArrayElementAsRef<int>(DrawCounters.GetUnsafePtr(), 0);
            ref var drawRangesCountRef = ref UnsafeUtility.ArrayElementAsRef<int>(DrawCounters.GetUnsafePtr(), 1);
            ref var drawCommandCountRef = ref UnsafeUtility.ArrayElementAsRef<int>(DrawCounters.GetUnsafePtr(), 2);

            Interlocked.Increment(ref drawRangesCountRef);
            Interlocked.Add(ref drawCommandCountRef, validSubBatchCount);
            Interlocked.Add(ref visibleCountRef, visibleCountPerBatchGroup);

            drawRangeDataRef.Count = validSubBatchCount;
            
            for (var i = index + 1; i < BatchGroups.Length; i++) // prefix sum
            {
                ref var nextDrawRangeDataRef = ref UnsafeUtility.ArrayElementAsRef<BatchGroupDrawRange>(DrawRangesData.GetUnsafePtr(), i);
                Interlocked.Add(ref nextDrawRangeDataRef.Begin, validSubBatchCount);
                Interlocked.Add(ref nextDrawRangeDataRef.VisibleIndexOffset, visibleCountPerBatchGroup);
            }
        }
    }
}