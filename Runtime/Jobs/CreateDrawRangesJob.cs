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
    internal struct CreateDrawRangesJob : IJobFor
    {
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchGroup> BatchGroups;
        [ReadOnly]
        public NativeArray<BatchGroupDrawRange> DrawRangeData;

        [NativeDisableUnsafePtrRestriction]
        public unsafe BatchCullingOutputDrawCommands* OutputDrawCommands;
        
        public unsafe void Execute(int index)
        {
            var drawRangeData = DrawRangeData[index];
            if(drawRangeData.Count == 0)
                return;
            
            var batchGroup = BatchGroups[index];
            var rendererDescription = batchGroup.BatchRendererData.Description;
            
            var drawRange = new BatchDrawRange
            {
                drawCommandsBegin = (uint) drawRangeData.Begin,
                drawCommandsCount = (uint) drawRangeData.Count,
                filterSettings = new BatchFilterSettings
                {
                    renderingLayerMask = rendererDescription.RenderingLayerMask,
                    layer = rendererDescription.Layer,
                    motionMode = rendererDescription.MotionMode,
                    shadowCastingMode = rendererDescription.ShadowCastingMode,
                    receiveShadows = rendererDescription.ReceiveShadows,
                    staticShadowCaster = rendererDescription.StaticShadowCaster,
                    allDepthSorted = false
                }
            };

            OutputDrawCommands->drawRanges[index + drawRangeData.IndexOffset] = drawRange;
        }
    }
}