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
        public NativeArray<BatchInstanceData> InstanceDataPerBatch;
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
            var windowCount = batchGroup.GetWindowCount();
            var visibleOffset = drawRangeData.VisibleIndexOffset;

            var batchStartIndex = drawRangeData.BatchIndex;
            for (var i = 0; i < windowCount; i++)
            {
                var batchIndex = batchStartIndex + i;
                var visibleCountPerBatch = VisibleCountPerBatch[batchIndex];
                if (visibleCountPerBatch == 0) // there is no any visible instances for this batch
                    continue;

                var batchInstanceData = InstanceDataPerBatch[batchIndex];
                UnsafeUtility.MemCpy((void*)((IntPtr) OutputDrawCommands->visibleInstances + visibleOffset * UnsafeUtility.SizeOf<int>()),
                    batchInstanceData.Indices, visibleCountPerBatch * UnsafeUtility.SizeOf<int>());

                visibleOffset += visibleCountPerBatch;
                UnsafeUtility.Free(batchInstanceData.Indices, Allocator.TempJob);
                batchInstanceData.Indices = null;
                
                InstanceDataPerBatch[batchIndex] = batchInstanceData;
            }
        }
    }
}