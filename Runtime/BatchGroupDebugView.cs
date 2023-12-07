namespace BrgContainer.Runtime
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.Rendering;

    internal sealed class BatchGroupDebugView
    {
        private readonly BatchGroup m_BatchGroup;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public unsafe BatchID[] BatchIds
        {
            get
            {
                if (!m_BatchGroup.IsCreated)
                    return null;
                
                var array = new BatchID[m_BatchGroup.Length];
                var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                UnsafeUtility.MemCpy((void*)gcHandle.AddrOfPinnedObject(), m_BatchGroup.GetUnsafePtr(), UnsafeUtility.SizeOf<BatchID>() * m_BatchGroup.Length);
                gcHandle.Free();
                return array;
            }
        }
        
        public BatchGroupDebugView(BatchGroup batchGroup)
        {
            m_BatchGroup = batchGroup;
        }
    }
}