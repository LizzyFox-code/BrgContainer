namespace BrgContainer.Runtime
{
    using Unity.Collections;
    using UnityEngine.Rendering;

    public delegate void SetGPUDataDelegate<T>(BatchID batchId, NativeArray<T> data, int nativeBufferStartIndex,
        int graphicsBufferStartIndex, int count) where T : unmanaged;
}