namespace BrgContainer.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
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
        private AllocatorManager.AllocatorHandle m_AllocatorHandle;
        internal BatchDescription m_BatchDescription;

        private DoubleBuffer<float4> m_Buffer;
        [NativeDisableUnsafePtrRestriction]
        private BatchID* m_Batches;
        
        public readonly int Length;
        public BatchRendererData BatchRendererData;

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.IsCreated;
        }

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
            get => m_Buffer.CurrentCount;
        }

        public BatchGroup(ref BatchDescription batchDescription, in BatchRendererData rendererData, AllocatorManager.AllocatorHandle allocatorHandle)
        {
            m_BatchDescription = batchDescription;
            BatchRendererData = rendererData;
            
            var bufferCapacity = m_BatchDescription.TotalBufferSize / 16;
            Length = m_BatchDescription.WindowCount;

            m_AllocatorHandle = allocatorHandle;

            var totalSize = CollectionHelper.Align(UnsafeUtility.SizeOf<BatchID>() * Length, CollectionHelper.CacheLineSize);
            m_Batches = (BatchID*)AllocatorManager.Allocate(allocatorHandle, totalSize, CollectionHelper.CacheLineSize);

            m_Buffer = new DoubleBuffer<float4>(bufferCapacity, m_BatchDescription.SizePerInstance / 16, allocatorHandle);
        }

        internal readonly DoubleBuffer<float4> GetDataBuffer()
        {
            return m_Buffer;
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

        public NativeArray<PackedMatrix> GetObjectToWorldArray(Allocator allocator)
        {
            var nativeArray = new NativeArray<PackedMatrix>(InstanceCount, allocator);
            var windowCount = this.GetWindowCount();

            var buffer = GetBuffer();
            
            for (var i = 0; i < windowCount; i++)
            {
                var instanceCountPerWindow = this.GetInstanceCountPerWindow(i);
                var sourceOffset = i * m_BatchDescription.AlignedWindowSize;
                var destinationOffset = i * m_BatchDescription.MaxInstancePerWindow * UnsafeUtility.SizeOf<PackedMatrix>();
                var size = instanceCountPerWindow * UnsafeUtility.SizeOf<PackedMatrix>();

                var sourcePtr = (void*) ((IntPtr) buffer + sourceOffset);
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
            if((IntPtr)m_Batches == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
#endif

            if (m_AllocatorHandle > Allocator.None)
            {
                m_Buffer.Dispose();
                AllocatorManager.Free(m_AllocatorHandle, m_Batches);

                m_BatchDescription.Dispose();
                BatchRendererData.Dispose();

                m_AllocatorHandle = Allocator.Invalid;
            }
            
            m_Batches = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_AllocatorHandle == Allocator.Invalid)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} can not be Disposed because it was not allocated with a valid allocator.");
            if((IntPtr)m_Batches == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
#endif
            
            if (m_AllocatorHandle > Allocator.None)
            {
                var disposeData = new BatchGroupDisposeData
                {
                    Buffer = m_Batches,
                    AllocatorHandle = m_AllocatorHandle
                };
                
                var jobHandle = new BatchGroupDisposeJob(ref disposeData).Schedule(inputDeps);
                jobHandle = m_Buffer.Dispose(jobHandle);

                m_Batches = null;

                m_AllocatorHandle = Allocator.Invalid;
                return JobHandle.CombineDependencies(jobHandle, m_BatchDescription.Dispose(inputDeps), BatchRendererData.Dispose(inputDeps));
            }
            
            m_Batches = null;
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
        
        private readonly float4* GetBuffer()
        {
            return m_Buffer.GetUnsafePointer();
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