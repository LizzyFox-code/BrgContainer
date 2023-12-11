namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;
    using UnityEngine.Rendering;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool IsBatchAliveDelegate(ContainerId containerId, BatchID batchID);
}