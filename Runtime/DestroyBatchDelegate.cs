namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;
    using UnityEngine.Rendering;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DestroyBatchDelegate(ContainerId containerId, BatchID batchId);
}