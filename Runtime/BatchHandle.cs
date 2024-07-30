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
    public struct BatchHandle
    {
        private readonly ContainerId m_ContainerId;
        internal readonly BatchID m_BatchId;
        
        private DoubleBuffer<float4> m_Buffer;
        [NativeDisableContainerSafetyRestriction]
        private readonly BatchDescription m_Description;
        
        private readonly FunctionPointer<UploadDelegate> m_UploadCallback;
        private readonly FunctionPointer<DestroyBatchDelegate> m_DestroyCallback;
        private readonly FunctionPointer<IsBatchAliveDelegate> m_IsAliveCallback;
        
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_UploadCallback.IsCreated && m_DestroyCallback.IsCreated && m_IsAliveCallback.IsCreated;
        }

        public bool IsAlive
        {
            [BurstDiscard]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsCreated && CheckIfIsAlive(m_ContainerId, m_BatchId);
        }

        public int InstanceCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.Count;
        }
        
        // TODO: rewrite this, may be use batch group directly?
        [ExcludeFromBurstCompatTesting("BatchHandle creating is unburstable")]
        internal BatchHandle(ContainerId containerId, BatchID batchId, DoubleBuffer<float4> buffer, ref BatchDescription description, FunctionPointer<UploadDelegate> uploadCallback, 
            FunctionPointer<DestroyBatchDelegate> destroyCallback, FunctionPointer<IsBatchAliveDelegate> isAliveCallback)
        {
            m_ContainerId = containerId;
            m_BatchId = batchId;
            
            m_Buffer = buffer;
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
            return new BatchInstanceDataBuffer(m_Buffer, m_Description.m_MetadataInfoMap, m_Description.m_MetadataValues,
                m_Description.MaxInstanceCount, m_Description.MaxInstancePerWindow, m_Description.AlignedWindowSize / 16);
        }
        
        /// <summary>
        /// Upload current data to the GPU side.
        /// </summary>
        /// <param name="instanceCount"></param>
        [BurstDiscard]
        [Obsolete("This method will be removed from public API soon.")]
        public void Upload(int instanceCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(instanceCount < 0 || instanceCount > m_Description.MaxInstanceCount)
                throw new ArgumentOutOfRangeException($"{nameof(instanceCount)} must be from 0 to {m_Description.MaxInstanceCount}.");
            
            if(!IsAlive)
                throw new InvalidOperationException("This batch already has been destroyed.");
#endif

            var buffer = m_Buffer.AsNativeArray();
            
            var completeWindows = instanceCount / m_Description.MaxInstancePerWindow;
            if (completeWindows > 0)
            {
                var size = completeWindows * m_Description.AlignedWindowSize / 16;
                Upload(m_ContainerId, m_BatchId, buffer, 0, 0, size);
            }

            var lastBatchId = completeWindows;
            var itemInLastBatch = instanceCount - m_Description.MaxInstancePerWindow * completeWindows;

            if (itemInLastBatch > 0)
            {
                var windowOffsetInFloat4 = lastBatchId * m_Description.AlignedWindowSize / 16;

                var offset = 0;
                for (var i = 0; i < m_Description.Length; i++)
                {
                    var metadataValue = m_Description[i];
                    var metadataInfo = m_Description.GetMetadataInfo(metadataValue.NameID);
                    var startIndex = windowOffsetInFloat4 + m_Description.MaxInstancePerWindow * offset;
                    var sizeInFloat4 = metadataInfo.Size / 16;
                    offset += sizeInFloat4;

                    Upload(m_ContainerId, m_BatchId, buffer, startIndex, startIndex,
                        itemInLastBatch * sizeInFloat4);
                }
            }
            
            SwapBuffers();
        }
        
        /// <summary>
        /// Upload current data to the GPU side.
        /// </summary>
        [BurstDiscard]
        public void Upload()
        {
            Upload(m_Buffer.Count);
        }

        /// <summary>
        /// Destroy the batch.
        /// </summary>
        [BurstDiscard]
        public void Destroy()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(!IsAlive)
                throw new InvalidOperationException("This batch already has been destroyed.");
#endif
            
            Destroy(m_ContainerId, m_BatchId);
        }
        
        private unsafe void SwapBuffers()
        {
            var destination = m_Buffer.GetCurrentUnsafePointer();
            var source = m_Buffer.GetUnsafePointer();

            var instanceCount = m_Buffer.Count;
            
            var completeWindows = instanceCount / m_Description.MaxInstancePerWindow;
            if (completeWindows > 0)
            {
                var size = completeWindows * m_Description.AlignedWindowSize / 16;
                UnsafeUtility.MemCpy(destination, source, size);
            }
            
            var lastBatchId = completeWindows;
            var itemInLastBatch = instanceCount - m_Description.MaxInstancePerWindow * completeWindows;
            if (itemInLastBatch > 0)
            {
                var windowOffsetInFloat4 = lastBatchId * m_Description.AlignedWindowSize / 16;

                var offset = 0;
                for (var i = 0; i < m_Description.Length; i++)
                {
                    var metadataValue = m_Description[i];
                    var metadataInfo = m_Description.GetMetadataInfo(metadataValue.NameID);
                    var startIndex = windowOffsetInFloat4 + m_Description.MaxInstancePerWindow * offset;
                    var sizeInFloat4 = metadataInfo.Size / 16;
                    offset += sizeInFloat4;

                    var bufferOffset = UnsafeUtility.SizeOf<float4>() * startIndex;
                    UnsafeUtility.MemCpy((byte*)destination + bufferOffset, (byte*)source + bufferOffset,
                        itemInLastBatch * sizeInFloat4 * UnsafeUtility.SizeOf<float4>());
                }
            }
            
            m_Buffer.Apply();
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