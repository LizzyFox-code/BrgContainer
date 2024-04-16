namespace BrgContainer.Runtime
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine.Rendering;

    /// <summary>
    /// The handle of a batch.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BatchHandle
    {
        private readonly ContainerId m_ContainerId;
        internal readonly BatchID m_BatchId;
        
        [NativeDisableContainerSafetyRestriction]
        private readonly NativeArray<float4> m_Buffer;
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe int* m_InstanceCount;
        [NativeDisableContainerSafetyRestriction]
        private readonly BatchDescription m_Description;
        
        private readonly FunctionPointer<UploadDelegate> m_UploadCallback;
        private readonly FunctionPointer<DestroyBatchDelegate> m_DestroyCallback;
        private readonly FunctionPointer<IsBatchAliveDelegate> m_IsAliveCallback;

        public bool IsCreated => m_UploadCallback.IsCreated && m_DestroyCallback.IsCreated && m_IsAliveCallback.IsCreated;
        public bool IsAlive => IsCreated && CheckIfIsAlive(m_ContainerId, m_BatchId);
        public unsafe int InstanceCount => (IntPtr)m_InstanceCount == IntPtr.Zero ? 0 : *m_InstanceCount;

        [ExcludeFromBurstCompatTesting("BatchHandle creating is unburstable")]
        internal unsafe BatchHandle(ContainerId containerId, BatchID batchId, NativeArray<float4> buffer, int* instanceCount, ref BatchDescription description, 
            FunctionPointer<UploadDelegate> uploadCallback, FunctionPointer<DestroyBatchDelegate> destroyCallback, FunctionPointer<IsBatchAliveDelegate> isAliveCallback)
        {
            m_ContainerId = containerId;
            m_BatchId = batchId;
            
            m_Buffer = buffer;
            m_InstanceCount = instanceCount;
            m_Description = description;
            
            m_UploadCallback = uploadCallback;
            m_DestroyCallback = destroyCallback;
            m_IsAliveCallback = isAliveCallback;
        }

        /// <summary>
        /// Returns <see cref="BatchInstanceDataBuffer"/> instance that provides API for write instance data.
        /// </summary>
        /// <returns>Returns <see cref="BatchInstanceDataBuffer"/> instance.</returns>
        public unsafe BatchInstanceDataBuffer AsInstanceDataBuffer()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(!IsAlive)
                throw new InvalidOperationException("This batch has been destroyed.");
#endif
            
            return new BatchInstanceDataBuffer(m_Buffer, m_Description.m_MetadataInfoMap, m_Description.m_MetadataValues,
                m_InstanceCount, m_Description.MaxInstanceCount, m_Description.MaxInstancePerWindow, m_Description.AlignedWindowSize / 16);
        }

        /// <summary>
        /// Upload current data to the GPU side.
        /// </summary>
        /// <param name="instanceCount"></param>
        public unsafe void Upload(int instanceCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(instanceCount < 0 || instanceCount > m_Description.MaxInstanceCount)
                throw new ArgumentOutOfRangeException($"{nameof(instanceCount)} must be from 0 to {m_Description.MaxInstanceCount}.");
            
            if(!IsAlive)
                throw new InvalidOperationException("This batch already has been destroyed.");
#endif
            
            *m_InstanceCount = instanceCount;
            
            var completeWindows = instanceCount / m_Description.MaxInstancePerWindow;
            if (completeWindows > 0)
            {
                var size = completeWindows * m_Description.AlignedWindowSize / 16;
                Upload(m_ContainerId, m_BatchId, m_Buffer, 0, 0, size);
            }

            var lastBatchId = completeWindows;
            var itemInLastBatch = instanceCount - m_Description.MaxInstancePerWindow * completeWindows;

            if (itemInLastBatch <= 0)
                return;
            
            var windowOffsetInFloat4 = lastBatchId * m_Description.AlignedWindowSize / 16;

            var offset = 0;
            for (var i = 0; i < m_Description.Length; i++)
            {
                var metadataValue = m_Description[i];
                var metadataInfo = m_Description.GetMetadataInfo(metadataValue.NameID);
                var startIndex = windowOffsetInFloat4 + m_Description.MaxInstancePerWindow * offset;
                var sizeInFloat4 = metadataInfo.Size / 16;
                offset += sizeInFloat4;

                Upload(m_ContainerId, m_BatchId, m_Buffer, startIndex, startIndex,
                    itemInLastBatch * sizeInFloat4);
            }
        }
        
        /// <summary>
        /// Upload current data to the GPU side.
        /// </summary>
        public unsafe void Upload()
        {
            Upload(*m_InstanceCount);
        }

        /// <summary>
        /// Destroy the batch.
        /// </summary>
        [ExcludeFromBurstCompatTesting("BatchHandle destroying is unburstable")]
        public void Destroy()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(!IsAlive)
                throw new InvalidOperationException("This batch already has been destroyed.");
#endif
            
            Destroy(m_ContainerId, m_BatchId);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Upload(ContainerId containerId, BatchID batchId, NativeArray<float4> data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count)
        {
            unsafe
            {
                ((delegate * unmanaged[Cdecl] <ContainerId, BatchID, NativeArray<float4>, int, int, int, void>)m_UploadCallback.Value)(containerId, batchId, data, 
                    nativeBufferStartIndex, graphicsBufferStartIndex, count);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Destroy(ContainerId containerId, BatchID batchId)
        {
            unsafe
            {
                ((delegate * unmanaged[Cdecl] <ContainerId, BatchID, void>)m_DestroyCallback.Value)(containerId, batchId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckIfIsAlive(ContainerId containerId, BatchID batchId)
        {
            bool isAlive;
            unsafe
            {
                isAlive = ((delegate * unmanaged[Cdecl] <ContainerId, BatchID, bool>)m_IsAliveCallback.Value)(containerId, batchId);
            }

            return isAlive;
        }
    }
}