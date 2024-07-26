namespace BrgContainer.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Lod;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Length}, InstanceCount = {InstanceCount}")]
    [DebuggerTypeProxy(typeof(BatchGroupDebugView))]
    public unsafe struct BatchGroup : INativeDisposable, IEnumerable<BatchID>
    {
        internal BatchDescription m_BatchDescription;

        [NativeDisableUnsafePtrRestriction]
        private float4* m_FirstBuffer;
        [NativeDisableUnsafePtrRestriction]
        private float4* m_SecondBuffer;
        [NativeDisableUnsafePtrRestriction]
        internal bool* m_BufferFlag;
        
        [NativeDisableUnsafePtrRestriction]
        private BatchID* m_Batches;
        [NativeDisableUnsafePtrRestriction]
        internal int* m_InstanceCount;
        
        public readonly int Length;
        private readonly int m_BufferLength;
        private AllocatorManager.AllocatorHandle m_AllocatorHandle;
        
        public BatchRendererData BatchRendererData;

        public readonly bool IsCreated => (IntPtr) m_FirstBuffer != IntPtr.Zero;

        public readonly BatchID this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException(nameof(index));
#endif
                
                return m_Batches[index];
            }
        }

        public readonly int InstanceCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *m_InstanceCount;
        }

        public BatchGroup(ref BatchDescription batchDescription, in BatchRendererData rendererData, AllocatorManager.AllocatorHandle allocatorHandle)
        {
            m_BatchDescription = batchDescription;
            BatchRendererData = rendererData;
            
            m_BufferLength = m_BatchDescription.TotalBufferSize / 16;
            Length = m_BatchDescription.WindowCount;

            m_AllocatorHandle = allocatorHandle;

            var totalSize = CalculateTotalSize(m_BufferLength, Length, out var secondBufferOffset,
                out var bufferFlagOffset, out var batchesOffset, out var instanceCountOffset);

            var data = (byte*)AllocatorManager.Allocate(allocatorHandle, totalSize, CollectionHelper.CacheLineSize);
            
            m_FirstBuffer = (float4*)data;
            m_SecondBuffer = (float4*)(data + secondBufferOffset);
            m_BufferFlag = (bool*)(data + bufferFlagOffset);

            m_Batches = (BatchID*)(data + batchesOffset);
            m_InstanceCount = (int*)(data + instanceCountOffset);
            
            UnsafeUtility.MemClear(m_InstanceCount, UnsafeUtility.SizeOf<int>());
        }

        public readonly NativeArray<float4> GetFirstDataBuffer()
        {
            var array = CollectionHelper.ConvertExistingDataToNativeArray<float4>(m_FirstBuffer, m_BufferLength,
                m_AllocatorHandle);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_AllocatorHandle == Allocator.Temp ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create());
#endif

            return array;
        }

        public readonly NativeArray<float4> GetSecondDataBuffer()
        {
            var array = CollectionHelper.ConvertExistingDataToNativeArray<float4>(m_SecondBuffer, m_BufferLength,
                m_AllocatorHandle);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_AllocatorHandle == Allocator.Temp ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create());
#endif

            return array;
        }

        [BurstDiscard]
        public void Register([NotNull]BatchRendererGroup batchRendererGroup, GraphicsBufferHandle bufferHandle)
        {
            var metadataValues = m_BatchDescription.AsNativeArray();
            for (var i = 0; i < m_BatchDescription.WindowCount; i++)
            {
                var offset = (uint) (i * m_BatchDescription.AlignedWindowSize);
                var batchId = batchRendererGroup.AddBatch(metadataValues, bufferHandle, offset, m_BatchDescription.WindowSize);
                m_Batches[i] = batchId;
            }
        }
        
        [BurstDiscard]
        public void Unregister([NotNull]BatchRendererGroup batchRendererGroup)
        {
            for (var i = 0; i < Length; i++)
            {
                batchRendererGroup.RemoveBatch(m_Batches[i]);
            }

            for (var i = 0; i < FixedBatchLodRendererData.Count; i++)
            {
                var lodRendererData = BatchRendererData[i];
                
                if(lodRendererData.MaterialID != BatchMaterialID.Null)
                    batchRendererGroup.UnregisterMaterial(lodRendererData.MaterialID);
                if(lodRendererData.MeshID != BatchMeshID.Null)
                    batchRendererGroup.UnregisterMesh(lodRendererData.MeshID);
            }
        }

        public void SetInstanceCount(int instanceCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(instanceCount < 0 || instanceCount > m_BatchDescription.MaxInstanceCount)
                throw new ArgumentOutOfRangeException($"Instance count {instanceCount} out of range from 0 to {m_BatchDescription.MaxInstanceCount} (include).");
#endif
            
            Interlocked.Exchange(ref *m_InstanceCount, instanceCount);
        }

        public NativeArray<PackedMatrix> GetObjectToWorldArray(Allocator allocator)
        {
            var nativeArray = new NativeArray<PackedMatrix>(InstanceCount, allocator);
            var windowCount = this.GetWindowCount();

            for (var i = 0; i < windowCount; i++)
            {
                var instanceCountPerWindow = this.GetInstanceCountPerWindow(i);
                var sourceOffset = i * m_BatchDescription.AlignedWindowSize;
                var destinationOffset = i * m_BatchDescription.MaxInstancePerWindow * UnsafeUtility.SizeOf<PackedMatrix>();
                var size = instanceCountPerWindow * UnsafeUtility.SizeOf<PackedMatrix>();

                var sourcePtr = (void*) ((IntPtr) m_FirstBuffer + sourceOffset);
                var destinationPtr = (void*) ((IntPtr) nativeArray.GetUnsafePtr() + destinationOffset);
                
                UnsafeUtility.MemCpy(destinationPtr, sourcePtr, size);
            }

            return nativeArray;
        }
        
        public readonly BatchID* GetUnsafePtr()
        {
            return m_Batches;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_AllocatorHandle == Allocator.Invalid)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} can not be Disposed because it was not allocated with a valid allocator.");
            if((IntPtr)m_FirstBuffer == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
#endif

            if (m_AllocatorHandle > Allocator.None)
            {
                AllocatorManager.Free(m_AllocatorHandle, m_FirstBuffer);

                m_BatchDescription.Dispose();
                BatchRendererData.Dispose();

                m_AllocatorHandle = Allocator.Invalid;
            }

            m_FirstBuffer = null;
            m_SecondBuffer = null;
            m_BufferFlag = null;
            m_Batches = null;
            m_InstanceCount = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_AllocatorHandle == Allocator.Invalid)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} can not be Disposed because it was not allocated with a valid allocator.");
            if((IntPtr)m_FirstBuffer == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
#endif
            
            if (m_AllocatorHandle > Allocator.None)
            {
                var disposeData = new BatchGroupDisposeData
                {
                    Buffer = m_FirstBuffer,
                    AllocatorHandle = m_AllocatorHandle
                };
                
                var jobHandle = new BatchGroupDisposeJob(ref disposeData).Schedule(inputDeps);
                
                m_FirstBuffer = null;
                m_SecondBuffer = null;
                m_BufferFlag = null;
                m_Batches = null;
                m_InstanceCount = null;

                m_AllocatorHandle = Allocator.Invalid;
                return JobHandle.CombineDependencies(jobHandle, m_BatchDescription.Dispose(inputDeps), BatchRendererData.Dispose(inputDeps));
            }

            m_FirstBuffer = null;
            m_SecondBuffer = null;
            m_BufferFlag = null;
            m_Batches = null;
            m_InstanceCount = null;

            return inputDeps;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        
        IEnumerator<BatchID> IEnumerable<BatchID>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        private static int CalculateTotalSize(int bufferLength, int batchCount, out int secondBufferOffset, out int bufferFlagOffset,
            out int batchesOffset, out int instanceCountOffset)
        {
            var sizeOfFloat4 = UnsafeUtility.SizeOf<float4>();
            var sizeOfBool = UnsafeUtility.SizeOf<bool>();
            var sizeOfBatchId = UnsafeUtility.SizeOf<BatchID>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var lengthOfFirstBuffer = CollectionHelper.Align(sizeOfFloat4 * bufferLength, CollectionHelper.CacheLineSize);
            var lengthOfSecondBuffer = CollectionHelper.Align(sizeOfFloat4 * bufferLength, CollectionHelper.CacheLineSize);
            var lengthOfBufferFlag = CollectionHelper.Align(sizeOfBool, CollectionHelper.CacheLineSize);
            var lengthOfBatches = CollectionHelper.Align(sizeOfBatchId * batchCount, CollectionHelper.CacheLineSize);
            var lengthOfInstanceCount = CollectionHelper.Align(sizeOfInt, CollectionHelper.CacheLineSize);

            var totalSize = lengthOfFirstBuffer + lengthOfSecondBuffer + lengthOfBufferFlag + lengthOfBatches +
                            lengthOfInstanceCount;
            secondBufferOffset = 0 + lengthOfFirstBuffer;
            bufferFlagOffset = secondBufferOffset + lengthOfSecondBuffer;
            batchesOffset = bufferFlagOffset + lengthOfBufferFlag;
            instanceCountOffset = batchesOffset + lengthOfBatches;

            return totalSize;
        }
        
        public struct Enumerator : IEnumerator<BatchID>
        {
            private readonly BatchGroup m_BatchGroup;
            private int m_Index;

            public BatchID Current => m_BatchGroup[m_Index];

            object IEnumerator.Current => Current;

            public Enumerator(BatchGroup batchGroup)
            {
                m_BatchGroup = batchGroup;
                m_Index = -1;
            }
            
            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_BatchGroup.Length;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}