namespace BrgContainer.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Jobs;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
#if ENABLE_IL2CPP
    using Il2Cpp;
#endif

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    public sealed class BatchRendererGroupContainer : IDisposable
    {
        private static readonly Dictionary<ContainerId, BatchRendererGroupContainer> m_Containers;
        private static long m_ContainerGlobalId;
        
        private readonly BatchRendererGroup m_BatchRendererGroup;

        private readonly Dictionary<BatchID, GraphicsBuffer> m_GraphicsBuffers; // per group
        private NativeHashMap<BatchID, BatchGroup> m_Groups;

        private readonly ContainerId m_ContainerId;

        private readonly FunctionPointer<UploadDelegate> m_UploadFunctionPointer;
        private readonly FunctionPointer<DestroyBatchDelegate> m_DestroyBatchFunctionPointer;
        private readonly FunctionPointer<IsBatchAliveDelegate> m_IsBatchAliveFunctionPointer;

        static BatchRendererGroupContainer()
        {
            m_Containers = new Dictionary<ContainerId, BatchRendererGroupContainer>();
        }

        /// <summary>
        /// Create an instance of the <see cref="BatchRendererGroupContainer"/>.
        /// </summary>
        /// <param name="bounds">The center and the size of the global batch bounding box.</param>
        public BatchRendererGroupContainer(Bounds bounds) : this()
        {
            m_BatchRendererGroup.SetGlobalBounds(bounds);
        }

        /// <summary>
        /// Create an instance of the <see cref="BatchRendererGroupContainer"/>.
        /// </summary>
        public BatchRendererGroupContainer()
        {
            m_ContainerId = new ContainerId(Interlocked.Increment(ref m_ContainerGlobalId));
            
            m_BatchRendererGroup = new BatchRendererGroup(CullingCallback, IntPtr.Zero);
            m_GraphicsBuffers = new Dictionary<BatchID, GraphicsBuffer>();
            m_Groups = new NativeHashMap<BatchID, BatchGroup>(1, Allocator.Persistent);
            
            m_UploadFunctionPointer = new FunctionPointer<UploadDelegate>(Marshal.GetFunctionPointerForDelegate(new UploadDelegate(UploadCallback)));
            m_DestroyBatchFunctionPointer = new FunctionPointer<DestroyBatchDelegate>(Marshal.GetFunctionPointerForDelegate(new DestroyBatchDelegate(DestroyBatchCallback)));
            m_IsBatchAliveFunctionPointer = new FunctionPointer<IsBatchAliveDelegate>(Marshal.GetFunctionPointerForDelegate(new IsBatchAliveDelegate(IsAliveCallback)));
            
            m_Containers.Add(m_ContainerId, this);
        }

        /// <summary>
        /// Set the bounds of the BatchRendererGroup. The bounds should encapsulate the render bounds
        /// of every object rendered with this BatchRendererGroup. Unity uses these bounds internally for culling.
        /// </summary>
        /// <param name="bounds">The center and the size of the global batch bounding box.</param>
        public void SetGlobalBounds(Bounds bounds)
        {
            m_BatchRendererGroup.SetGlobalBounds(bounds);
        }

        /// <summary>
        /// Add a new batch with a description, mesh, material and a renderer description.
        /// </summary>
        /// <param name="batchDescription">A batch description provides a batch metadata.</param>
        /// <param name="mesh">A mesh that will be rendering with this batch.</param>
        /// <param name="subMeshIndex">A subMesh index for a mesh.</param>
        /// <param name="material">A mesh material.</param>
        /// <param name="rendererDescription">A renderer description provides a rendering metadata.</param>
        /// <returns>Returns a batch handle that provides some API for write and upload instance data for the GPU.</returns>
        public unsafe BatchHandle AddBatch(ref BatchDescription batchDescription, [NotNull]Mesh mesh, ushort subMeshIndex, [NotNull]Material material, ref RendererDescription rendererDescription)
        {
            var graphicsBuffer = CreateGraphicsBuffer(BatchDescription.IsUBO, batchDescription.TotalBufferSize);
            var rendererData = CreateRendererData(mesh, subMeshIndex, material, ref rendererDescription);
            var batchGroup = CreateBatchGroup(ref batchDescription, rendererData, graphicsBuffer.bufferHandle);
            
            var batchId = batchGroup[0];
            m_GraphicsBuffers.Add(batchId, graphicsBuffer);
            m_Groups.Add(batchId, batchGroup);

            return new BatchHandle(m_ContainerId, batchId, batchGroup.GetNativeBuffer(), batchGroup.m_InstanceCount, 
                ref batchDescription, m_UploadFunctionPointer, m_DestroyBatchFunctionPointer, m_IsBatchAliveFunctionPointer);
        }

        /// <summary>
        /// Remove the exist batch.
        /// </summary>
        /// <param name="batchHandle"></param>
        public void RemoveBatch(in BatchHandle batchHandle)
        {
            DestroyBatchCallback(m_ContainerId, batchHandle.m_BatchId);
        }

        /// <summary>
        /// Dispose this batch renderer group container.
        /// </summary>
        public void Dispose()
        {
            foreach (var group in m_Groups)
            {
                group.Value.Unregister(m_BatchRendererGroup);
                group.Value.Dispose();
            }
            
            m_Groups.Dispose();
            m_BatchRendererGroup.Dispose();

            foreach (var graphicsBuffer in m_GraphicsBuffers.Values)
                graphicsBuffer.Dispose();
            
            m_GraphicsBuffers.Clear();

            m_Containers.Remove(m_ContainerId);
        }
        
        [AOT.MonoPInvokeCallback(typeof(UploadDelegate))]
        private static void UploadCallback(ContainerId containerId, BatchID batchID, NativeArray<float4> data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count)
        {
            if(!m_Containers.TryGetValue(containerId, out var container))
                return;

            container.m_GraphicsBuffers[batchID].SetData(data, nativeBufferStartIndex, graphicsBufferStartIndex, count);
        }
        
        [AOT.MonoPInvokeCallback(typeof(DestroyBatchDelegate))]
        private static void DestroyBatchCallback(ContainerId containerId, BatchID batchID)
        {
            if(!m_Containers.TryGetValue(containerId, out var container))
                return;
            
            if(container.m_Groups.TryGetValue(batchID, out var batchGroup))
            {
                container.m_Groups.Remove(batchID);
                batchGroup.Unregister(container.m_BatchRendererGroup);
                batchGroup.Dispose();
            }
            
            if(container.m_GraphicsBuffers.Remove(batchID, out var graphicsBuffer))
                graphicsBuffer.Dispose();
        }
        
        [AOT.MonoPInvokeCallback(typeof(IsBatchAliveDelegate))]
        private static bool IsAliveCallback(ContainerId containerId, BatchID batchId)
        {
            if(!m_Containers.TryGetValue(containerId, out var container))
                return false;
            
            return container.m_Groups.ContainsKey(batchId);
        }

        private GraphicsBuffer CreateGraphicsBuffer(bool isUbo, int bufferSize)
        {
            var target = isUbo ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Raw;
            var count = isUbo ? bufferSize / 16 : bufferSize / 4;
            var stride = isUbo ? 16 : 4;

            return new GraphicsBuffer(target, count, stride);
        }

        private BatchGroup CreateBatchGroup(ref BatchDescription batchDescription, in BatchRendererData rendererData, GraphicsBufferHandle graphicsBufferHandle)
        {
            var batchGroup = new BatchGroup(ref batchDescription, rendererData, Allocator.Persistent);
            batchGroup.Register(m_BatchRendererGroup, graphicsBufferHandle);

            return batchGroup;
        }

        private BatchRendererData CreateRendererData([NotNull]Mesh mesh, ushort subMeshIndex, [NotNull]Material material, ref RendererDescription description)
        {
            var meshId = m_BatchRendererGroup.RegisterMesh(mesh);
            var materialId = m_BatchRendererGroup.RegisterMaterial(material);
            
            return new BatchRendererData(meshId, materialId, subMeshIndex, mesh.bounds.extents, ref description);
        }

        [BurstCompile]
        private unsafe JobHandle CullingCallback(BatchRendererGroup renderergroup, BatchCullingContext cullingcontext,
            BatchCullingOutput cullingoutput, IntPtr usercontext)
        {
            cullingoutput.drawCommands[0] = new BatchCullingOutputDrawCommands();
            
            var batchGroups = m_Groups.GetValueArray(Allocator.TempJob);

            var batchCount = 0;
            for (var i = 0; i < batchGroups.Length; i++)
                batchCount += batchGroups[i].GetWindowCount(); // sub batch count (for UBO)
            
            if(batchCount == 0)
                return batchGroups.Dispose(default);

            var visibleIndicesPerBatch = new NativeArray<BatchInstanceIndices>(batchCount, Allocator.TempJob);
            var visibleCountPerBatch = new NativeArray<int>(batchCount, Allocator.TempJob);

            var batchJobHandles = stackalloc JobHandle[batchCount];
            for (var i = 0; i < batchGroups.Length; i++)
            {
                var batchGroup = batchGroups[i];
                var maxInstancePerWindow = batchGroup.m_BatchDescription.MaxInstancePerWindow;
                var windowCount = batchGroup.GetWindowCount();
                var objectToWorld = batchGroup.GetObjectToWorldArray(Allocator.TempJob);

                JobHandle batchHandle = default;
                for (var b = 0; b < windowCount; b++)
                {
                    var instanceCountPerBatch = batchGroup.GetInstanceCountPerWindow(b);
                    var visibleIndices = new NativeList<int>(instanceCountPerBatch, Allocator.TempJob);
                    var cullingJob = new CullingBatchInstancesJob
                    {
                        CullingPlanes = cullingcontext.cullingPlanes,
                        ObjectToWorld = objectToWorld,
                        Extents = batchGroup.BatchRendererData.Extents,
                        DataOffset = maxInstancePerWindow * b
                    };
                    var jobHandle = cullingJob.ScheduleAppendByRef(visibleIndices, instanceCountPerBatch, batchHandle);

                    var copyJob = new CopyVisibleIndicesToMapJob
                    {
                        VisibleIndicesPerBatch = visibleIndicesPerBatch,
                        VisibleIndices = visibleIndices,
                        VisibleCountPerChunk = visibleCountPerBatch,
                        BatchIndex = i + b
                    };
                    batchHandle = copyJob.ScheduleByRef(jobHandle);
                    batchHandle = visibleIndices.Dispose(batchHandle);
                }

                batchJobHandles[i] = objectToWorld.Dispose(batchHandle);
            }

            var cullingHandle = JobHandleUnsafeUtility.CombineDependencies(batchJobHandles, batchCount);

            var drawCounters = new NativeArray<int>(3, Allocator.TempJob);
            var drawRangeData = new NativeArray<BatchGroupDrawRange>(batchGroups.Length, Allocator.TempJob);
            
            var computeDrawCountersJob = new ComputeDrawCountersJob
            {
                DrawCounters = drawCounters,
                VisibleCountPerBatch = visibleCountPerBatch,
                DrawRangesData = drawRangeData,
                BatchGroups = batchGroups
            };
            var computeDrawCountersHandle = computeDrawCountersJob.ScheduleParallelByRef(batchGroups.Length, 32, cullingHandle);

            var allocateOutputDrawCommandsJob = new AllocateOutputDrawCommandsJob
            {
                OutputDrawCommands = (BatchCullingOutputDrawCommands*) cullingoutput.drawCommands.GetUnsafePtr(),
                Counters = drawCounters
            };
            var allocateOutputDrawCommandsHandle = allocateOutputDrawCommandsJob.ScheduleByRef(computeDrawCountersHandle);
            allocateOutputDrawCommandsHandle = drawCounters.Dispose(allocateOutputDrawCommandsHandle); 

            var createDrawRangesJob = new CreateDrawRangesJob
            {
                BatchGroups = batchGroups,
                DrawRangeData = drawRangeData,
                OutputDrawCommands = (BatchCullingOutputDrawCommands*) cullingoutput.drawCommands.GetUnsafePtr(),
            };
            var createDrawRangesHandle = createDrawRangesJob.ScheduleParallelByRef(batchGroups.Length, 64, allocateOutputDrawCommandsHandle);

            var createDrawCommandsJob = new CreateDrawCommandsJob
            {
                BatchGroups = batchGroups,
                DrawRangeData = drawRangeData,
                VisibleCountPerBatch = visibleCountPerBatch,
                OutputDrawCommands = (BatchCullingOutputDrawCommands*) cullingoutput.drawCommands.GetUnsafePtr()
            };
            var createDrawCommandsHandle = createDrawCommandsJob.ScheduleParallelByRef(batchGroups.Length, 64, createDrawRangesHandle);

            var copyVisibilityIndicesToArrayJob = new CopyVisibilityIndicesToArrayJob
            {
                BatchGroups = batchGroups,
                VisibleCountPerBatch = visibleCountPerBatch,
                VisibleIndicesPerBatch = visibleIndicesPerBatch,
                DrawRangesData = drawRangeData,
                OutputDrawCommands = (BatchCullingOutputDrawCommands*) cullingoutput.drawCommands.GetUnsafePtr()
            };

            var resultHandle = copyVisibilityIndicesToArrayJob.ScheduleParallelByRef(batchGroups.Length, 32, createDrawCommandsHandle);

            return JobHandle.CombineDependencies(JobHandle.CombineDependencies(visibleIndicesPerBatch.Dispose(resultHandle), visibleCountPerBatch.Dispose(resultHandle)), 
                drawRangeData.Dispose(resultHandle), batchGroups.Dispose(resultHandle));
        }
    }
}