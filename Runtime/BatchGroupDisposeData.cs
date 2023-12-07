namespace BrgContainer.Runtime
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    internal unsafe struct BatchGroupDisposeData : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* Buffer;
        [NativeDisableUnsafePtrRestriction]
        internal void* Batches;
        [NativeDisableUnsafePtrRestriction]
        internal void* InstanceCount;
        
        internal Allocator AllocatorLabel;

        public void Dispose()
        {
            UnsafeUtility.FreeTracked(Buffer, AllocatorLabel);
            UnsafeUtility.FreeTracked(Batches, AllocatorLabel);
            UnsafeUtility.FreeTracked(InstanceCount, AllocatorLabel);
        }
    }
}