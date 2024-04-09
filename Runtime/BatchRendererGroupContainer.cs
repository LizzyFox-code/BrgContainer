namespace BrgContainer.Runtime
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Jobs;
    using Lod;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
    using LODGroup = Lod.LODGroup;
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
        private static readonly ConcurrentDictionary<ContainerId, BatchRendererGroupContainer> m_Containers;
        private static long m_ContainerGlobalId;
        
        private readonly BatchRendererGroup m_BatchRendererGroup;

        private readonly Dictionary<BatchID, GraphicsBuffer> m_GraphicsBuffers; // per group
        private NativeHashMap<BatchID, BatchGroup> m_Groups;

        private readonly ContainerId m_ContainerId;

        private static readonly FunctionPointer<UploadDelegate> m_UploadFunctionPointer;
        private static readonly FunctionPointer<DestroyBatchDelegate> m_DestroyBatchFunctionPointer;
        private static readonly FunctionPointer<IsBatchAliveDelegate> m_IsBatchAliveFunctionPointer;

        static BatchRendererGroupContainer()
        {
            m_Containers = new ConcurrentDictionary<ContainerId, BatchRendererGroupContainer>();
            
            m_UploadFunctionPointer = new FunctionPointer<UploadDelegate>(Marshal.GetFunctionPointerForDelegate(new UploadDelegate(UploadCallback)));
            m_DestroyBatchFunctionPointer = new FunctionPointer<DestroyBatchDelegate>(Marshal.GetFunctionPointerForDelegate(new DestroyBatchDelegate(DestroyBatchCallback)));
            m_IsBatchAliveFunctionPointer = new FunctionPointer<IsBatchAliveDelegate>(Marshal.GetFunctionPointerForDelegate(new IsBatchAliveDelegate(IsAliveCallback)));
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

            m_Containers.TryAdd(m_ContainerId, this);
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
        /// Add a new batch with description, mesh, material and renderer description.
        /// </summary>
        /// <param name="batchDescription">A batch description provides a batch metadata.</param>
        /// <param name="mesh">A mesh that will be rendering with this batch.</param>
        /// <param name="subMeshIndex">A subMesh index for a mesh.</param>
        /// <param name="material">A mesh material.</param>
        /// <param name="rendererDescription">A renderer description provides a rendering metadata.</param>
        /// <returns>Returns a batch handle that provides some API for write and upload instance data for the GPU.</returns>
        public BatchHandle AddBatch(ref BatchDescription batchDescription, [NotNull] Mesh mesh, ushort subMeshIndex, [NotNull] Material material, in RendererDescription rendererDescription)
        {
            return AddBatch(ref batchDescription, mesh, subMeshIndex, material, float3.zero, in rendererDescription);
        }

        /// <summary>
        /// Add a new batch with description, mesh, material and renderer description.
        /// </summary>
        /// <param name="batchDescription">A batch description provides a batch metadata.</param>
        /// <param name="mesh">A mesh that will be rendering with this batch.</param>
        /// <param name="subMeshIndex">A subMesh index for a mesh.</param>
        /// <param name="material">A mesh material.</param>
        /// <param name="extentsOffset"></param>
        /// <param name="rendererDescription">A renderer description provides a rendering metadata.</param>
        /// <returns>Returns a batch handle that provides some API for write and upload instance data for the GPU.</returns>
        public BatchHandle AddBatch(ref BatchDescription batchDescription, [NotNull]Mesh mesh, ushort subMeshIndex, [NotNull]Material material, float3 extentsOffset, in RendererDescription rendererDescription)
        {
            var lodGroup = new LODGroup
            {
                LODs = new[]
                {
                    new LODMeshData
                    {
                        Mesh = mesh,
                        Material = material,
                        SubMeshIndex = subMeshIndex,
                        ScreenRelativeTransitionHeight = 0.0f
                    }
                }
            };
            return AddBatch(ref batchDescription, ref lodGroup, extentsOffset, rendererDescription);
        }

        /// <summary>
        /// Adds a batch to the BatchRendererGroupContainer.
        /// </summary>
        /// <param name="batchDescription">The batch description.</param>
        /// <param name="lodGroup">The LOD group.</param>
        /// <param name="extentsOffset">The extents offset.</param>
        /// <param name="rendererDescription">The renderer description.</param>
        /// <returns>The batch handle.</returns>
        public unsafe BatchHandle AddBatch(ref BatchDescription batchDescription, ref LODGroup lodGroup, float3 extentsOffset, in RendererDescription rendererDescription)
        {
#if (UNITY_EDITOR || DEVELOPMENT_BUILD)
            if (lodGroup.LODs.Length == 0)
                throw new InvalidOperationException("Lod group must have at least 1 lod, but here is no one!");
#endif
            
            var graphicsBuffer = CreateGraphicsBuffer(BatchDescription.IsUBO, batchDescription.TotalBufferSize);
            var rendererData = CreateRendererData(ref lodGroup, extentsOffset, rendererDescription);
            var batchGroup = CreateBatchGroup(ref batchDescription, ref rendererData, graphicsBuffer.bufferHandle);
            
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
        /// Get batch data by a batch handle.
        /// </summary>
        /// <param name="batchHandle"></param>
        /// <param name="batchDescription"></param>
        /// <param name="batchRendererData"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void GetBatchData(in BatchHandle batchHandle, out BatchDescription batchDescription, out BatchRendererData batchRendererData)
        {
            if(!m_Groups.TryGetValue(batchHandle.m_BatchId, out var batchGroup))
                throw new InvalidOperationException("Batch handle is not alive.");

            batchDescription = batchGroup.m_BatchDescription;
            batchRendererData = batchGroup.BatchRendererData;
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

            m_Containers.TryRemove(m_ContainerId, out _);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GraphicsBuffer CreateGraphicsBuffer(bool isUbo, int bufferSize)
        {
            var target = isUbo ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Raw;
            var count = isUbo ? bufferSize / 16 : bufferSize / 4;
            var stride = isUbo ? 16 : 4;

            return new GraphicsBuffer(target, count, stride);
        }

        private BatchGroup CreateBatchGroup(ref BatchDescription batchDescription, ref BatchRendererData rendererData, GraphicsBufferHandle graphicsBufferHandle)
        {
            var batchGroup = new BatchGroup(ref batchDescription, rendererData, Allocator.Persistent);
            batchGroup.Register(m_BatchRendererGroup, graphicsBufferHandle);

            return batchGroup;
        }

        private BatchRendererData CreateRendererData(ref LODGroup lodGroup, float3 extentsOffset, in RendererDescription description)
        {
            var lodGroupLODs = lodGroup.LODs;
            var batchLodDescription = new BatchLodDescription(lodGroupLODs.Length);
            for (var i = 0; i < lodGroupLODs.Length; ++i)
            {
                batchLodDescription[i] = lodGroupLODs[i].ScreenRelativeTransitionHeight;
            }

            var extents = new UnsafeList<float3>(lodGroup.LODs.Length, Allocator.Persistent);
            for (var i = 0; i < lodGroup.LODs.Length; i++)
            {
                var lod = lodGroup.LODs[i];
                var lodExtents = float3.zero;
                if (lod.Mesh != null)
                    lodExtents = new float3(lod.Mesh.bounds.extents) + extentsOffset;
                
                extents.Add(lodExtents);
            }
            
            var batchRendererData = new BatchRendererData(ref extents, description, ref batchLodDescription);
            for (var i = 0; i < lodGroup.LODs.Length; i++)
            {
                var lodData = lodGroup.LODs[i];
                if(lodData.Mesh == null || lodData.Material == null)
                {
                    batchRendererData[i] = default;
                    continue;
                }
                
                var meshId = m_BatchRendererGroup.RegisterMesh(lodData.Mesh);
                var materialId = m_BatchRendererGroup.RegisterMaterial(lodData.Material);

                batchRendererData[i] = new BatchLodRendererData(meshId, materialId, lodData.SubMeshIndex);
            }

            return batchRendererData;
        }

        [BurstCompile]
        private unsafe JobHandle CullingCallback(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            cullingOutput.drawCommands[0] = new BatchCullingOutputDrawCommands();
            
            var batchGroups = m_Groups.GetValueArray(Allocator.TempJob);

            var batchCount = 0;
            for (var i = 0; i < batchGroups.Length; i++)
                batchCount += batchGroups[i].GetWindowCount(); // sub batch count (for UBO)
            
            if(batchCount == 0)
                return batchGroups.Dispose(default);

            var instanceDataPerBatch = new NativeArray<BatchInstanceData>(batchCount, Allocator.TempJob);
            var visibleCountPerBatch = new NativeArray<int>(batchCount, Allocator.TempJob);

            var offset = 0;
            var batchJobHandles = stackalloc JobHandle[batchGroups.Length];
            for (var batchGroupIndex = 0; batchGroupIndex < batchGroups.Length; batchGroupIndex++)
            {
                var batchGroup = batchGroups[batchGroupIndex];
                var maxInstancePerWindow = batchGroup.m_BatchDescription.MaxInstancePerWindow;
                var windowCount = batchGroup.GetWindowCount();
                var objectToWorld = batchGroup.GetObjectToWorldArray(Allocator.TempJob);

                JobHandle batchHandle = default;
                for (var batchIndex = 0; batchIndex < windowCount; batchIndex++)
                {
                    var instanceCountPerBatch = batchGroup.GetInstanceCountPerWindow(batchIndex);
                    var visibleIndices = new NativeList<int>(instanceCountPerBatch, Allocator.TempJob);
                    
                    var lodPerInstance = new NativeArray<int>(instanceCountPerBatch, Allocator.TempJob);
                    var instanceCountPerLod = new NativeArray<int>(FixedBatchLodRendererData.Count, Allocator.TempJob);

                    var extents = batchGroup.BatchRendererData.Extents;
                    var lodParams = LODParams.CreateLODParams(cullingContext.lodParameters);
                    var selectLodPerInstanceJob = new SelectLodPerInstanceJob
                    {
                        ObjectToWorld = objectToWorld,
                        LodPerInstance = lodPerInstance,
                        LodDescription = batchGroup.BatchRendererData.BatchLodDescription,
                        DataOffset = maxInstancePerWindow * batchIndex,
                        Extents = extents,
                        LODParams = lodParams
                    };
                    var selectLodPerInstanceJobHandle = selectLodPerInstanceJob.ScheduleAppendByRef(visibleIndices, instanceCountPerBatch, batchHandle);
                    
                    var cullingBatchInstancesJob = new CullingBatchInstancesJob
                    {
                        CullingPlanes = cullingContext.cullingPlanes,
                        ObjectToWorld = objectToWorld,
                        LodPerInstance = lodPerInstance,
                        InstanceCountPerLod = instanceCountPerLod,
                        Extents = extents,
                        DataOffset = maxInstancePerWindow * batchIndex
                    };
                    var cullingBatchInstancesJobHandle = cullingBatchInstancesJob.ScheduleFilterByRef(visibleIndices, selectLodPerInstanceJobHandle);
                    
                    var sortJob = new SimpleSortJob<int, IndexComparer>
                    {
                        Array = visibleIndices.AsDeferredJobArray(),
                        Comparer = new IndexComparer(lodPerInstance)
                    };
                    var sortJobHandle = sortJob.ScheduleByRef(cullingBatchInstancesJobHandle); // sort by LOD
                    sortJobHandle = lodPerInstance.Dispose(sortJobHandle);

                    var copyVisibleIndicesToMapJob = new CopyVisibleIndicesToMapJob
                    {
                        InstanceDataPerBatch = instanceDataPerBatch,
                        VisibleIndices = visibleIndices.AsDeferredJobArray(),
                        InstanceCountPerLod = instanceCountPerLod,
                        VisibleCountPerChunk = visibleCountPerBatch,
                        BatchIndex = offset + batchIndex
                    };
                    batchHandle = copyVisibleIndicesToMapJob.ScheduleByRef(sortJobHandle);
                    batchHandle = instanceCountPerLod.Dispose(batchHandle);
                    batchHandle = visibleIndices.Dispose(batchHandle);
                }

                offset += windowCount;
                batchJobHandles[batchGroupIndex] = objectToWorld.Dispose(batchHandle);
            }

            var cullingHandle = JobHandleUnsafeUtility.CombineDependencies(batchJobHandles, batchGroups.Length);

            var drawCounters = new NativeArray<int>(3, Allocator.TempJob);
            var drawRangeData = new NativeArray<BatchGroupDrawRange>(batchGroups.Length, Allocator.TempJob);

            offset = 0;
            for (var i = 0; i < batchGroups.Length; i++)
            {
                var batchGroup = batchGroups[i];
                var windowCount = batchGroup.GetWindowCount();
                
                var computeDrawCountersJob = new ComputeDrawCountersJob
                {
                    DrawCounters = drawCounters,
                    VisibleCountPerBatch = visibleCountPerBatch,
                    DrawRangesData = drawRangeData,
                    BatchGroups = batchGroups,
                    BatchGroupIndex = i,
                    BatchOffset = offset,
                    InstanceDataPerBatch = instanceDataPerBatch
                };
                    
                offset += windowCount;
                batchJobHandles[i] = computeDrawCountersJob.ScheduleByRef(cullingHandle);
            }
            
            var countersHandle = JobHandleUnsafeUtility.CombineDependencies(batchJobHandles, batchGroups.Length);

            var drawCommands = (BatchCullingOutputDrawCommands*) cullingOutput.drawCommands.GetUnsafePtr();
            var allocateOutputDrawCommandsJob = new AllocateOutputDrawCommandsJob
            {
                OutputDrawCommands = drawCommands,
                Counters = drawCounters
            };
            var allocateOutputDrawCommandsHandle = allocateOutputDrawCommandsJob.ScheduleByRef(countersHandle);
            allocateOutputDrawCommandsHandle = drawCounters.Dispose(allocateOutputDrawCommandsHandle);

            var createDrawRangesJob = new CreateDrawRangesJob
            {
                BatchGroups = batchGroups,
                DrawRangeData = drawRangeData,
                OutputDrawCommands = drawCommands
            };
            var createDrawRangesHandle = createDrawRangesJob.ScheduleParallelByRef(batchGroups.Length, 64, allocateOutputDrawCommandsHandle);

            var createDrawCommandsJob = new CreateDrawCommandsJob
            {
                BatchGroups = batchGroups,
                DrawRangeData = drawRangeData,
                VisibleCountPerBatch = visibleCountPerBatch,
                InstanceDataPerBatch = instanceDataPerBatch,
                OutputDrawCommands = drawCommands
            };
            var createDrawCommandsHandle = createDrawCommandsJob.ScheduleParallelByRef(batchGroups.Length, 64, createDrawRangesHandle);

            var copyVisibilityIndicesToArrayJob = new CopyVisibilityIndicesToArrayJob
            {
                BatchGroups = batchGroups,
                VisibleCountPerBatch = visibleCountPerBatch,
                InstanceDataPerBatch = instanceDataPerBatch,
                DrawRangesData = drawRangeData,
                OutputDrawCommands = drawCommands
            };

            var resultHandle = copyVisibilityIndicesToArrayJob.ScheduleParallelByRef(batchGroups.Length, 32, createDrawCommandsHandle);
            resultHandle = JobHandle.CombineDependencies(instanceDataPerBatch.Dispose(resultHandle),
                visibleCountPerBatch.Dispose(resultHandle));
            resultHandle = JobHandle.CombineDependencies(drawRangeData.Dispose(resultHandle), batchGroups.Dispose(resultHandle));

            return resultHandle;
        }
    }
}