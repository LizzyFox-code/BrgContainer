namespace BrgContainer.Runtime
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.Rendering;

    internal sealed class BatchDescriptionDebugView
    {
        private readonly BatchDescription m_BatchDescription;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe MetadataValue[] MetadataValues
        {
            get
            {
                if (!m_BatchDescription.IsCreated)
                    return null;
                
                var array = new MetadataValue[m_BatchDescription.Length];
                var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                UnsafeUtility.MemCpy((void*)gcHandle.AddrOfPinnedObject(), m_BatchDescription.AsNativeArray().GetUnsafePtr(), 
                    UnsafeUtility.SizeOf<BatchID>() * m_BatchDescription.Length);
                gcHandle.Free();
                return array;
            }
        }
        
        public BatchDescriptionDebugView(BatchDescription batchDescription)
        {
            m_BatchDescription = batchDescription;
        }
    }
}