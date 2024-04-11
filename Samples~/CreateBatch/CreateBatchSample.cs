namespace Samples.Create_Batch
{
    using System.Collections.Generic;
    using Unity.Collections;
    using UnityEngine;
    using BrgContainer.Runtime;
    
    [DisallowMultipleComponent]
    public sealed class CreateBatchSample : MonoBehaviour
    {
        [SerializeField]
        private ObjectEntry[] m_ObjectEntries;
        
        private BatchRendererGroupContainer m_Container;

        private readonly List<BatchHandle> m_Handles = new List<BatchHandle>();

        private void Awake()
        {
            var boundsCenter = Vector3.zero;
            var boundsSize = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            // by these bounds BRG culling by default, so if we want to draw the all objects we need to create a bounds with float.MaxValue size
            var bounds = new Bounds(boundsCenter, boundsSize);
            
            // create a BRG container
            m_Container = new BatchRendererGroupContainer(bounds);

            // create a batch per object entry
            foreach (var objectEntry in m_ObjectEntries)
            {
                var batchHandle = CreateBatch(objectEntry);
                m_Handles.Add(batchHandle);
            }
        }

        private BatchHandle CreateBatch(ObjectEntry entry)
        {
            var batchDescription = new BatchDescription(entry.MaxCount, Allocator.Persistent);
            var rendererDescription = new RendererDescription(entry.ShadowCastingMode, entry.ReceiveShadows,
                entry.StaticShadowCaster, 1, 0, MotionVectorGenerationMode.Camera);

            return m_Container.AddBatch(ref batchDescription, entry.Mesh, 0, entry.Material, rendererDescription);
        }

        private void OnDestroy()
        {
            // we must dispose the BRG container
            m_Container.Dispose();
        }
    }
}