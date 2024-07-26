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

        internal AllocatorManager.AllocatorHandle AllocatorHandle;

        public void Dispose()
        {
            AllocatorManager.Free(AllocatorHandle, Buffer);
        }
    }
}