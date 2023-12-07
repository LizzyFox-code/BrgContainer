namespace BrgContainer.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// The data buffer of instances. Burst compatible.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Instance Count = {InstanceCount}")]
    public readonly struct BatchInstanceDataBuffer : IEquatable<BatchInstanceDataBuffer>
    {
        [NativeDisableParallelForRestriction]
        private readonly NativeArray<float4> m_Buffer;
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe UnsafeHashMap<int, MetadataInfo>* m_MetadataInfo;
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe int* m_InstanceCountReference;

        private readonly int m_MaxInstanceCount;
        private readonly int m_MaxInstancePerWindow;
        private readonly int m_WindowSizeInFloat4;

        /// <summary>
        /// Current instance count.
        /// </summary>
        public unsafe int InstanceCount => *m_InstanceCountReference;

        internal unsafe BatchInstanceDataBuffer(NativeArray<float4> buffer, UnsafeHashMap<int, MetadataInfo>* metadataInfo, 
            int* instanceCountReference, int maxInstanceCount, int maxInstancePerWindow, int windowSizeInFloat4)
        {
            m_Buffer = buffer;
            m_MetadataInfo = metadataInfo;
            m_InstanceCountReference = instanceCountReference;
            m_MaxInstanceCount = maxInstanceCount;

            m_MaxInstancePerWindow = maxInstancePerWindow;
            m_WindowSizeInFloat4 = windowSizeInFloat4;
        }

        /// <summary>
        /// Set instance data by the property id and the instance index.
        /// </summary>
        /// <param name="index">The instance index.</param>
        /// <param name="propertyId">The material property id.</param>
        /// <param name="itemData">The instance data.</param>
        /// <typeparam name="T">The blittable type.</typeparam>
        public unsafe void WriteInstanceData<T>(int index, int propertyId, T itemData) where T : unmanaged
        {
            var windowId = Math.DivRem(index, m_MaxInstancePerWindow, out var i);
            var windowOffsetInFloat4 = windowId * m_WindowSizeInFloat4;
            var metadataInfo = (*m_MetadataInfo)[propertyId];
            var sizeInFloat4 = metadataInfo.Size / 16;
            var offsetInFloat4 = metadataInfo.Offset / 16;

            var bufferIndex = windowOffsetInFloat4 + offsetInFloat4 + i * sizeInFloat4;
            UnsafeUtility.CopyStructureToPtr(ref itemData, (void*) ((IntPtr) m_Buffer.GetUnsafePtr() + bufferIndex * UnsafeUtility.SizeOf<float4>()));
        }

        /// <summary>
        /// Get instance data by the property id and the instance index.
        /// </summary>
        /// <param name="index">The instance index.</param>
        /// <param name="propertyId">The material property id.</param>
        /// <typeparam name="T">The blittable type.</typeparam>
        /// <returns>Returns instance data by the property id.</returns>
        public unsafe T ReadInstanceData<T>(int index, int propertyId) where T : unmanaged
        {
            var windowId = Math.DivRem(index, m_MaxInstancePerWindow, out var i);
            var windowOffsetInFloat4 = windowId * m_WindowSizeInFloat4;
            var metadataInfo = (*m_MetadataInfo)[propertyId];
            var sizeInFloat4 = metadataInfo.Size / 16;
            var offsetInFloat4 = metadataInfo.Offset / 16;
            var bufferIndex = windowOffsetInFloat4 + offsetInFloat4 + i * sizeInFloat4;
            
            UnsafeUtility.CopyPtrToStructure((void*) ((IntPtr) m_Buffer.GetUnsafePtr() + bufferIndex * UnsafeUtility.SizeOf<float4>()), out T item);
            return item;
        }

        /// <summary>
        /// Set current instance count.
        /// </summary>
        /// <param name="instanceCount">Instance count.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public unsafe void SetInstanceCount(int instanceCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(instanceCount < 0 || instanceCount > m_MaxInstanceCount)
                throw new ArgumentOutOfRangeException($"Instance count {instanceCount} out of range from 0 to {m_MaxInstanceCount} (include).");
#endif
            
            Interlocked.Exchange(ref *m_InstanceCountReference, instanceCount);
        }

        public bool Equals(BatchInstanceDataBuffer other)
        {
            return m_Buffer.Equals(other.m_Buffer);
        }

        public override bool Equals(object obj)
        {
            return obj is BatchInstanceDataBuffer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Buffer.GetHashCode();
        }
    }
}