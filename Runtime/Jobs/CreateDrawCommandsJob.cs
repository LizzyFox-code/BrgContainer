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
    internal struct CreateDrawCommandsJob : IJobFor
    {
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchGroup> BatchGroups;
        [ReadOnly]
        public NativeArray<BatchGroupDrawRange> DrawRangeData;
        [ReadOnly]
        public NativeArray<int> VisibleCountPerBatch;

        [NativeDisableUnsafePtrRestriction]
        public unsafe BatchCullingOutputDrawCommands* OutputDrawCommands;
        
        public unsafe void Execute(int index)
        {
            var drawRangeData = DrawRangeData[index];
            if(drawRangeData.Count == 0)
                return;

            var batchGroup = BatchGroups[index];
            var subBatchCount = batchGroup.GetWindowCount();

            var batchIndex = drawRangeData.Begin;
            var visibleOffset = drawRangeData.VisibleIndexOffset;
            for (var i = 0; i < subBatchCount; i++)
            {
                var visibleCountPerBatch = VisibleCountPerBatch[batchIndex];
                if(visibleCountPerBatch == 0) // there is no any visible instances for this batch
                    continue;
                
                var batchDrawCommand = new BatchDrawCommand
                {
                    visibleOffset = (uint) visibleOffset,
                    visibleCount = (uint) visibleCountPerBatch,
                    batchID = batchGroup[i],
                    materialID = batchGroup.BatchRendererData.MaterialID,
                    meshID = batchGroup.BatchRendererData.MeshID,
                    submeshIndex = batchGroup.BatchRendererData.SubMeshIndex,
                    splitVisibilityMask = 0xff,
                    flags = BatchDrawCommandFlags.None,
                    sortingPosition = 0
                };

                OutputDrawCommands->drawCommands[batchIndex] = batchDrawCommand;

                batchIndex++;
                visibleOffset += visibleCountPerBatch;
            }
        }
    }
}