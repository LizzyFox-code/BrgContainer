namespace BrgContainer.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine.Rendering;

    /// <summary>
    /// The data buffer of instances. Burst compatible.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("InstanceCount = {InstanceCount}")]
    public readonly struct BatchInstanceDataBuffer : IEquatable<BatchInstanceDataBuffer>
    {
        [NativeDisableParallelForRestriction]
        private readonly NativeArray<float4> m_Buffer;
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe UnsafeHashMap<int, MetadataInfo>* m_MetadataInfo;
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe UnsafeList<MetadataValue>* m_MetadataValues;
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe int* m_InstanceCountReference;

        public readonly int Capacity;
        private readonly int m_MaxInstancePerWindow;
        private readonly int m_WindowSizeInFloat4;

        /// <summary>
        /// Current instance count.
        /// </summary>
        public unsafe int InstanceCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *m_InstanceCountReference;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetInstanceCount(value);
        }

        internal unsafe BatchInstanceDataBuffer(NativeArray<float4> buffer, UnsafeHashMap<int, MetadataInfo>* metadataInfo, UnsafeList<MetadataValue>* metadataValues,
            int* instanceCountReference, int maxInstanceCount, int maxInstancePerWindow, int windowSizeInFloat4)
        {
            m_Buffer = buffer;
            m_MetadataInfo = metadataInfo;
            m_MetadataValues = metadataValues;
            m_InstanceCountReference = instanceCountReference;
            Capacity = maxInstanceCount;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(index < 0 || index >= InstanceCount)
                throw new IndexOutOfRangeException($"Index {index} must be from 0 to {InstanceCount} (exclude).");
#endif
            
            var windowId = Math.DivRem(index, m_MaxInstancePerWindow, out var i);
            var windowOffsetInFloat4 = windowId * m_WindowSizeInFloat4;
            var metadataInfo = (*m_MetadataInfo)[propertyId];
            var sizeInFloat4 = metadataInfo.Size / 16;
            var offsetInFloat4 = metadataInfo.Offset / 16;

            var elementIndex = windowOffsetInFloat4 + offsetInFloat4 + i * sizeInFloat4;
            UnsafeUtility.CopyStructureToPtr(ref itemData, (void*) ((IntPtr) m_Buffer.GetUnsafePtr() + elementIndex * UnsafeUtility.SizeOf<float4>()));
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(index < 0 || index >= InstanceCount)
                throw new IndexOutOfRangeException($"Index {index} must be from 0 to {InstanceCount} (exclude).");
#endif
            
            var windowId = Math.DivRem(index, m_MaxInstancePerWindow, out var i);
            var windowOffsetInFloat4 = windowId * m_WindowSizeInFloat4;
            var metadataInfo = (*m_MetadataInfo)[propertyId];
            var sizeInFloat4 = metadataInfo.Size / 16;
            var offsetInFloat4 = metadataInfo.Offset / 16;
            var elementIndex = windowOffsetInFloat4 + offsetInFloat4 + i * sizeInFloat4;
            
            UnsafeUtility.CopyPtrToStructure((void*) ((IntPtr) m_Buffer.GetUnsafePtr() + elementIndex * UnsafeUtility.SizeOf<float4>()), out T item);
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
            if(instanceCount < 0 || instanceCount > Capacity)
                throw new ArgumentOutOfRangeException($"Instance count {instanceCount} out of range from 0 to {Capacity} (include).");
#endif
            
            Interlocked.Exchange(ref *m_InstanceCountReference, instanceCount);
        }
        
        public unsafe void Remove(int index, int count)
        {
            var instanceCount = InstanceCount;
            if (index + count >= instanceCount)
            {
                InstanceCount = math.max(0, instanceCount - count);
                return;
            }
            
            var windowCount = (instanceCount + m_MaxInstancePerWindow - 1) / m_MaxInstancePerWindow;
            
            var startIndex = math.max(index, 0);
            var startWindowId = Math.DivRem(startIndex, m_MaxInstancePerWindow, out var startI);

            var endIndex = index + count;
            var endWindowId = Math.DivRem(endIndex, m_MaxInstancePerWindow, out var endI);

            for (var i = startWindowId; i < windowCount; i++)
            {
                var startWindowOffset = startWindowId * m_WindowSizeInFloat4;
                var endWindowOffset = endWindowId * m_WindowSizeInFloat4;

                var startInstancePerWindow = startWindowId == windowCount - 1 ? m_MaxInstancePerWindow - (windowCount * m_MaxInstancePerWindow - instanceCount) : m_MaxInstancePerWindow;
                var endInstancePerWindow = endWindowId == windowCount - 1 ? m_MaxInstancePerWindow - (windowCount * m_MaxInstancePerWindow - instanceCount) : m_MaxInstancePerWindow;

                var copyCount = math.min(startInstancePerWindow - startI, endInstancePerWindow - endI);
                
                for (var metadataIndex = 0; metadataIndex < (*m_MetadataValues).Length; metadataIndex++)
                {
                    var metadataValue = (*m_MetadataValues)[metadataIndex];
                    var metadataInfo = (*m_MetadataInfo)[metadataValue.NameID];
                    var sizeInFloat4 = metadataInfo.Size / 16;
                    var offsetInFloat4 = metadataInfo.Offset / 16;
                    
                    var destinationIndex = startWindowOffset + startI * sizeInFloat4 + offsetInFloat4;
                    var sourceIndex = endWindowOffset + endI * sizeInFloat4 + offsetInFloat4;
                    
                    var sourcePtr = (void*) ((IntPtr) m_Buffer.GetUnsafePtr() + sourceIndex * UnsafeUtility.SizeOf<float4>());
                    var destinationPtr = (void*) ((IntPtr) m_Buffer.GetUnsafePtr() + destinationIndex * UnsafeUtility.SizeOf<float4>());

                    var length = copyCount * sizeInFloat4 * UnsafeUtility.SizeOf<float4>();
                    
                    UnsafeUtility.MemMove(destinationPtr, sourcePtr, length);
                }
                
                startIndex += copyCount;
                startWindowId = Math.DivRem(startIndex, m_MaxInstancePerWindow, out startI);

                endIndex += copyCount;
                endWindowId = Math.DivRem(endIndex, m_MaxInstancePerWindow, out endI);
            }
            
            InstanceCount = math.max(0, instanceCount - count);
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