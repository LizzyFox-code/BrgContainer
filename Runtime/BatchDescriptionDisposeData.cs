namespace BrgContainer.Runtime
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    internal unsafe struct BatchDescriptionDisposeData : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* MetadataValues;
        [NativeDisableUnsafePtrRestriction]
        internal void* MetadataInfoMap;

        internal Allocator AllocatorLabel;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety; // need for job
#endif
        
        public void Dispose()
        {
            UnsafeUtility.FreeTracked(MetadataValues, AllocatorLabel);
            UnsafeUtility.FreeTracked(MetadataInfoMap, AllocatorLabel);
        }
    }
}