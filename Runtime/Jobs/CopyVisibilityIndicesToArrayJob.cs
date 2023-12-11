namespace BrgContainer.Runtime.Jobs
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct CopyVisibilityIndicesToArrayJob : IJobFor
    {
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchGroup> BatchGroups;
        [ReadOnly]
        public NativeArray<int> VisibleCountPerBatch;
        [ReadOnly]
        public NativeArray<BatchInstanceIndices> VisibleIndicesPerBatch;
        [ReadOnly]
        public NativeArray<BatchGroupDrawRange> DrawRangesData;

        [NativeDisableUnsafePtrRestriction]
        public unsafe BatchCullingOutputDrawCommands* OutputDrawCommands;
        
        public unsafe void Execute(int index)
        {
            var drawRangeData = DrawRangesData[index];
            if(drawRangeData.Count == 0)
                return; // there is no any visible batches
            
            var batchGroup = BatchGroups[index];
            var subBatchCount = batchGroup.GetWindowCount();
            var visibleOffset = drawRangeData.VisibleIndexOffset;
            
            for (var i = 0; i < subBatchCount; i++)
            {
                var visibleCountPerBatch = VisibleCountPerBatch[index + i];
                if (visibleCountPerBatch == 0) // there is no any visible instances for this batch
                    continue;

                var batchInstanceIndices = VisibleIndicesPerBatch[index + i];
                UnsafeUtility.MemCpy((void*)((IntPtr) OutputDrawCommands->visibleInstances + visibleOffset * UnsafeUtility.SizeOf<int>()),
                    batchInstanceIndices.Indices, visibleCountPerBatch * UnsafeUtility.SizeOf<int>());

                visibleOffset += visibleCountPerBatch;
                UnsafeUtility.Free(batchInstanceIndices.Indices, Allocator.TempJob);
                batchInstanceIndices.Indices = null;
                
                VisibleIndicesPerBatch[index + i] = batchInstanceIndices;
            }
        }
    }
}