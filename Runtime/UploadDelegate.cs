namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine.Rendering;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UploadDelegate(BatchID batchId, NativeArray<float4> data, int nativeBufferStartIndex,
        int graphicsBufferStartIndex, int count);
}