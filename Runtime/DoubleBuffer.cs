namespace BrgContainer.Runtime
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("IsCreated = {IsCreated}, Count = {Count}")]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new []{typeof(int)})]
    internal unsafe struct DoubleBuffer<T> : INativeDisposable
        where T : unmanaged
    {
        private AllocatorManager.AllocatorHandle m_AllocatorHandle;
        
        [NoAlias, NativeDisableUnsafePtrRestriction]
        private T* m_FirstBuffer;
        [NoAlias, NativeDisableUnsafePtrRestriction]
        private T* m_SecondBuffer;
        [NoAlias, NativeDisableUnsafePtrRestriction]
        private int* m_Count;
        [NoAlias, NativeDisableUnsafePtrRestriction]
        private int* m_BufferIndex;

        public readonly int Capacity;
        public readonly int LengthPerInstance;

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FirstBuffer != null;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => ReadCount();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => WriteCount(value);
        }

        public readonly int CurrentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ReadCurrentCount();
        }

        public DoubleBuffer(int capacity, int lengthPerInstance, AllocatorManager.AllocatorHandle allocatorHandle)
        {
            Capacity = capacity;
            LengthPerInstance = lengthPerInstance;
            m_AllocatorHandle = allocatorHandle;

            var totalSize = CalculateTotalSize(capacity, out var secondOffset, out var countOffset, out var indexOffset);
            var data = (byte*)AllocatorManager.Allocate(allocatorHandle, totalSize, CollectionHelper.CacheLineSize);

            m_FirstBuffer = (T*)data;
            m_SecondBuffer = (T*)(data + secondOffset);
            m_Count = (int*)(data + countOffset);
            m_BufferIndex = (int*)(data + indexOffset);
            
            UnsafeUtility.MemClear(m_Count, UnsafeUtility.SizeOf<int>() * 2);
            UnsafeUtility.MemClear(m_BufferIndex, UnsafeUtility.SizeOf<int>());
        }

        public void Apply()
        {
            var index = *m_BufferIndex;
            var count = UnsafeUtility.ReadArrayElement<int>(m_Count, index);
            
            if (index == 1)
                index = 0;
            else
                index = 1;
            
            *m_BufferIndex = index;
            UnsafeUtility.WriteArrayElement(m_Count, index, count);
        }
        
        public void Dispose()
        {
            if(!IsCreated)
                return;
            
            AllocatorManager.Free(m_AllocatorHandle, m_FirstBuffer);

            m_FirstBuffer = null;
            m_SecondBuffer = null;
            m_Count = null;
            m_BufferIndex = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
                return inputDeps;

            var disposeJob = new DisposeJob
            {
                Buffer = m_FirstBuffer,
                AllocatorHandle = m_AllocatorHandle
            };
            
            m_FirstBuffer = null;
            m_SecondBuffer = null;
            m_Count = null;
            m_BufferIndex = null;

            return disposeJob.ScheduleByRef(inputDeps);
        }

        public readonly T* GetUnsafePointer()
        {
            var index = *m_BufferIndex;
            if (index == 0)
                return m_FirstBuffer;

            return m_SecondBuffer;
        }
        
        public readonly T* GetCurrentUnsafePointer()
        {
            var index = *m_BufferIndex;
            if (index == 1)
                return m_FirstBuffer;

            return m_SecondBuffer;
        }

        public readonly NativeArray<T> AsNativeArray()
        {
            var temp = CollectionHelper.ConvertExistingDataToNativeArray<T>(GetUnsafePointer(), Capacity,
                m_AllocatorHandle);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref temp, m_AllocatorHandle == Allocator.Temp ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create());
#endif
            return temp;
        }

        private readonly int ReadCount()
        {
            var index = *m_BufferIndex;
            return UnsafeUtility.ReadArrayElement<int>(m_Count, index);
        }

        private void WriteCount(int value)
        {
            var index = *m_BufferIndex;
            UnsafeUtility.WriteArrayElement(m_Count, index, value);
        }

        private readonly int ReadCurrentCount()
        {
            var index = *m_BufferIndex;
            if(index == 0)
                return UnsafeUtility.ReadArrayElement<int>(m_Count, 1);
            
            return UnsafeUtility.ReadArrayElement<int>(m_Count, 0);
        }

        private static int CalculateTotalSize(int length, out int secondOffset, out int countOffset, out int indexOffset)
        {
            var sizeOfT = UnsafeUtility.SizeOf<T>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var lengthOfFirst = CollectionHelper.Align(sizeOfT * length, CollectionHelper.CacheLineSize);
            var lengthOfSecond = CollectionHelper.Align(sizeOfT * length, CollectionHelper.CacheLineSize);
            var lengthOfCount = CollectionHelper.Align(sizeOfInt * 2, CollectionHelper.CacheLineSize);
            var lengthOfIndex = CollectionHelper.Align(sizeOfInt, CollectionHelper.CacheLineSize);

            var totalSize = lengthOfFirst + lengthOfSecond + lengthOfCount + lengthOfIndex;
            secondOffset = 0 + lengthOfFirst;
            countOffset = secondOffset + lengthOfSecond;
            indexOffset = countOffset + lengthOfCount;

            return totalSize;
        }
        
        [BurstCompile]
        private struct DisposeJob : IJob
        {
            public void* Buffer;
            public AllocatorManager.AllocatorHandle AllocatorHandle;
            
            public void Execute()
            {
                AllocatorManager.Free(AllocatorHandle, Buffer);
            }
        }
    }
}