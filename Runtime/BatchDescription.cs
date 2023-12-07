namespace BrgContainer.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(BatchDescriptionDebugView))]
    [DebuggerDisplay("MaxInstancePerWindow = {MaxInstancePerWindow}, WindowCount = {WindowCount}, Length = {Length}, IsCreated = {IsCreated}")]
    public struct BatchDescription : IEnumerable<MetadataValue>, INativeDisposable
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static int m_StaticSafetyId;
        
        private AtomicSafetyHandle m_Safety; // this uses only for native array creation without allocations
#endif
        
        private static readonly int m_ObjectToWorldPropertyName = Shader.PropertyToID("unity_ObjectToWorld");
        private static readonly int m_WorldToObjectPropertyName = Shader.PropertyToID("unity_WorldToObject");
        
        private const uint PerInstanceBit = 0x80000000;
        public static readonly bool IsUBO = BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

        [NativeDisableUnsafePtrRestriction]
        private unsafe UnsafeList<MetadataValue>* m_MetadataValues;
        [NativeDisableUnsafePtrRestriction]
        internal unsafe UnsafeHashMap<int, MetadataInfo>* m_MetadataInfoMap;
        
        private Allocator m_Allocator;

        public readonly unsafe MetadataValue this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (*m_MetadataValues)[index];
        }

        public readonly unsafe bool IsCreated => (IntPtr)m_MetadataValues != IntPtr.Zero && 
                                                 (IntPtr)m_MetadataInfoMap != IntPtr.Zero;

        public readonly int Length;

        public readonly int MaxInstanceCount;
        public readonly int SizePerInstance;

        public readonly int AlignedWindowSize;
        public readonly int MaxInstancePerWindow;
        public readonly int WindowCount;
        public readonly uint WindowSize;
        public readonly int TotalBufferSize;

        [ExcludeFromBurstCompatTesting("BatchDescription creating is unburstable")]
        public unsafe BatchDescription(int maxInstanceCount, Allocator allocator)
        {
            MaxInstanceCount = maxInstanceCount;
            m_Allocator = allocator;
            Length = 2;
            
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = allocator == Allocator.Temp ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
            InitStaticSafetyId(ref m_Safety);
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, m_StaticSafetyId);
#endif

            m_MetadataValues = (UnsafeList<MetadataValue>*) UnsafeUtility.MallocTracked(
                UnsafeUtility.SizeOf<UnsafeList<MetadataValue>>(), UnsafeUtility.AlignOf<UnsafeList<MetadataValue>>(),
                allocator, 0);
            m_MetadataInfoMap = (UnsafeHashMap<int, MetadataInfo>*) UnsafeUtility.MallocTracked(
                UnsafeUtility.SizeOf<UnsafeHashMap<int, MetadataInfo>>(),
                UnsafeUtility.AlignOf<UnsafeHashMap<int, MetadataInfo>>(),
                allocator, 0);
            
            *m_MetadataValues = new UnsafeList<MetadataValue>(Length, allocator);
            *m_MetadataInfoMap = new UnsafeHashMap<int, MetadataInfo>(Length, allocator);

            SizePerInstance = UnsafeUtility.SizeOf<PackedMatrix>() * 2;

            if (IsUBO)
            {
                AlignedWindowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();
                MaxInstancePerWindow = AlignedWindowSize / SizePerInstance;
                WindowCount = (maxInstanceCount + MaxInstancePerWindow - 1) / MaxInstancePerWindow;
                WindowSize = (uint) AlignedWindowSize;
                TotalBufferSize = WindowCount * AlignedWindowSize;
            }
            else
            {
                AlignedWindowSize = (maxInstanceCount * SizePerInstance + 15) & -16;
                MaxInstancePerWindow = maxInstanceCount;
                WindowCount = 1;
                WindowSize = 0u;
                TotalBufferSize = WindowCount * AlignedWindowSize;
            }

            var metadataOffset = 0;
            RegisterMetadata(UnsafeUtility.SizeOf<PackedMatrix>(), m_ObjectToWorldPropertyName, ref metadataOffset);
            RegisterMetadata(UnsafeUtility.SizeOf<PackedMatrix>(), m_WorldToObjectPropertyName, ref metadataOffset);
        }
        
        [ExcludeFromBurstCompatTesting("BatchDescription creating is unburstable")]
        public unsafe BatchDescription(int maxInstanceCount, NativeArray<MaterialProperty> materialProperties, Allocator allocator)
        {
            MaxInstanceCount = maxInstanceCount;
            m_Allocator = allocator;
            Length = materialProperties.Length + 2;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = allocator == Allocator.Temp ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
            InitStaticSafetyId(ref m_Safety);
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, m_StaticSafetyId);
#endif

            m_MetadataValues = (UnsafeList<MetadataValue>*) UnsafeUtility.MallocTracked(
                UnsafeUtility.SizeOf<UnsafeList<MetadataValue>>(), UnsafeUtility.AlignOf<UnsafeList<MetadataValue>>(),
                allocator, 0);
            m_MetadataInfoMap = (UnsafeHashMap<int, MetadataInfo>*) UnsafeUtility.MallocTracked(
                UnsafeUtility.SizeOf<UnsafeHashMap<int, MetadataInfo>>(),
                UnsafeUtility.AlignOf<UnsafeHashMap<int, MetadataInfo>>(),
                allocator, 0);
            
            *m_MetadataValues = new UnsafeList<MetadataValue>(Length, allocator);
            *m_MetadataInfoMap = new UnsafeHashMap<int, MetadataInfo>(Length, allocator);

            SizePerInstance = UnsafeUtility.SizeOf<PackedMatrix>() * 2;
            for (var i = 0; i < materialProperties.Length; i++)
            {
                var size = (materialProperties[i].SizeInBytes + 15) & -16;
                SizePerInstance += size;
            }

            if (IsUBO)
            {
                AlignedWindowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();
                MaxInstancePerWindow = AlignedWindowSize / SizePerInstance;
                WindowCount = (maxInstanceCount + MaxInstancePerWindow - 1) / MaxInstancePerWindow;
                WindowSize = (uint) AlignedWindowSize;
                TotalBufferSize = WindowCount * AlignedWindowSize;
            }
            else
            {
                AlignedWindowSize = (maxInstanceCount * SizePerInstance + 15) & -16;
                MaxInstancePerWindow = maxInstanceCount;
                WindowCount = 1;
                WindowSize = 0u;
                TotalBufferSize = WindowCount * AlignedWindowSize;
            }

            var metadataOffset = 0;
            RegisterMetadata(UnsafeUtility.SizeOf<PackedMatrix>(), m_ObjectToWorldPropertyName, ref metadataOffset);
            RegisterMetadata(UnsafeUtility.SizeOf<PackedMatrix>(), m_WorldToObjectPropertyName, ref metadataOffset);

            for (var i = 0; i < materialProperties.Length; i++)
            {
                var materialProperty = materialProperties[i];
                
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if(m_MetadataInfoMap->ContainsKey(materialProperty.PropertyId))
                    throw new InvalidOperationException($"Property with id {materialProperty.PropertyId} has been registered yet.");
#endif
                var alignedSize = (materialProperty.SizeInBytes + 15) & -16;
                RegisterMetadata(alignedSize, materialProperty.PropertyId, ref metadataOffset, materialProperty.IsPerInstance);
            }
        }

        public readonly unsafe NativeArray<MetadataValue> AsNativeArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<MetadataValue>(m_MetadataValues->Ptr,
                m_MetadataValues->Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var arraySafety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
            
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
            return array;
        }

        internal readonly unsafe MetadataInfo GetMetadataInfo(int propertyId)
        {
            return (*m_MetadataInfoMap)[propertyId];
        }

        public readonly IEnumerator<MetadataValue> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        [ExcludeFromBurstCompatTesting("BatchDescription disposing is unburstable")]
        public unsafe void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_Allocator == Allocator.Invalid)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} can not be Disposed because it was not allocated with a valid allocator.");
            if((IntPtr)m_MetadataValues == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
            if((IntPtr)m_MetadataInfoMap == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
#endif
            
            if (m_Allocator > Allocator.None)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_Safety);
#endif
                
                (*m_MetadataValues).Dispose();
                (*m_MetadataInfoMap).Dispose();
                
                UnsafeUtility.FreeTracked(m_MetadataValues, m_Allocator);
                UnsafeUtility.FreeTracked(m_MetadataInfoMap, m_Allocator);

                m_Allocator = Allocator.Invalid;
            }

            m_MetadataValues = null;
            m_MetadataInfoMap = null;
        }

        [ExcludeFromBurstCompatTesting("BatchDescription disposing is unburstable")]
        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_Allocator == Allocator.Invalid)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} can not be Disposed because it was not allocated with a valid allocator.");
            if((IntPtr)m_MetadataValues == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
            if((IntPtr)m_MetadataInfoMap == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(BatchGroup)} is already disposed");
#endif
            
            if (m_Allocator > Allocator.None)
            {
                var disposeHandle = JobHandle.CombineDependencies((*m_MetadataValues).Dispose(inputDeps),
                    (*m_MetadataInfoMap).Dispose(inputDeps));
                
                var disposeData = new BatchDescriptionDisposeData
                {
                    MetadataValues = m_MetadataValues,
                    MetadataInfoMap = m_MetadataInfoMap,
                    AllocatorLabel = m_Allocator,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety
#endif
                };
                
                var jobHandle = new BatchDescriptionDisposeJob(ref disposeData).Schedule(disposeHandle);
                
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_Safety);
#endif
                
                m_MetadataValues = null;
                m_MetadataInfoMap = null;
                m_Allocator = Allocator.Invalid;
                
                return jobHandle;
            }

            m_MetadataValues = null;
            m_MetadataInfoMap = null;
            
            return inputDeps;
        }
        
        private unsafe void RegisterMetadata(int sizeInBytes, int propertyId, ref int metadataOffset, bool isPerInstance = true)
        {
            var metadataInfo = new MetadataInfo(sizeInBytes, metadataOffset, propertyId, isPerInstance);
            var metadataValue = new MetadataValue
            {
                NameID = propertyId,
                Value = (uint)metadataOffset | (isPerInstance ? PerInstanceBit : 0u)
            };
            
            m_MetadataValues->Add(metadataValue);
            m_MetadataInfoMap->Add(propertyId, metadataInfo);

            metadataOffset += sizeInBytes * MaxInstancePerWindow;
        }
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if(m_StaticSafetyId == 0)
                m_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<BatchDescription>();
                
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, m_StaticSafetyId);
        }
#endif
        
        public struct Enumerator : IEnumerator<MetadataValue>
        {
            private readonly BatchDescription m_BatchDescription;
            private int m_Index;

            public MetadataValue Current => m_BatchDescription[m_Index];

            object IEnumerator.Current => Current;

            public Enumerator(BatchDescription batchDescription)
            {
                m_BatchDescription = batchDescription;
                m_Index = -1;
            }
            
            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_BatchDescription.Length;
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