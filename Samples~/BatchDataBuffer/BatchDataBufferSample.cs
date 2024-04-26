namespace Samples.Batch_Data_Buffer
{
    using System.Collections.Generic;
    using BrgContainer.Runtime;
    using Unity.Collections;
    using UnityEngine;
    using Unity.Mathematics;
    using Random = UnityEngine.Random;
    
    [DisallowMultipleComponent]
    public sealed class BatchDataBufferSample : MonoBehaviour
    {
        [SerializeField]
        private ObjectEntry[] m_ObjectEntries;

        [SerializeField]
        private int m_BatchIndex;
        
        [SerializeField]
        private int m_AddInstanceCount = 10;
        [SerializeField]
        private int m_RemoveInstanceCount = 10;
        
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

                // get an instance data buffer that contains all data per instance
                var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();
                instanceDataBuffer.SetInstanceCount(instanceDataBuffer.Capacity); // set current instance count
                SetRandomDataForBatchDataBuffer(instanceDataBuffer,0, instanceDataBuffer.Capacity);
                
                batchHandle.Upload(); // upload to GPU side
            }
        }

        private BatchHandle CreateBatch(ObjectEntry entry)
        {
            var batchDescription = new BatchDescription(entry.MaxCount, Allocator.Persistent);
            var rendererDescription = new RendererDescription(entry.ShadowCastingMode, entry.ReceiveShadows,
                entry.StaticShadowCaster, 1, 0, MotionVectorGenerationMode.Camera);

            return m_Container.AddBatch(ref batchDescription, entry.Mesh, 0, entry.Material, rendererDescription);
        }
        
        [ContextMenu("Set Random Data")]
        private void SetRandomData()
        {
            foreach (var batchHandle in m_Handles)
            {
                var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();
                var instanceCount = Random.Range(0, instanceDataBuffer.Capacity);
                    
                instanceDataBuffer.SetInstanceCount(instanceCount); // set current instance count
                SetRandomDataForBatchDataBuffer(instanceDataBuffer,0, instanceCount);
                
                batchHandle.Upload(); // upload to GPU side
            }
        }

        [ContextMenu("Add instances")]
        private void AddInstances()
        {
            if(m_BatchIndex >= m_Handles.Count)
                return;

            var batchHandle = m_Handles[m_BatchIndex];
            var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();
            var instanceCount = math.min(instanceDataBuffer.Capacity, instanceDataBuffer.Capacity + m_AddInstanceCount);
            var from = instanceDataBuffer.InstanceCount;
                
            SetRandomDataForBatchDataBuffer(instanceDataBuffer, from, instanceCount);
        }

        // remove instances
        [ContextMenu("Remove instances")]
        private void RemoveInstances()
        {
            if(m_BatchIndex >= m_Handles.Count)
                return;

            var batchHandle = m_Handles[m_BatchIndex];
            var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();
            
            var from = instanceDataBuffer.InstanceCount;
            instanceDataBuffer.Remove(from, m_RemoveInstanceCount);
        }

        [ContextMenu("Log instance data")]
        private void LogInstanceData()
        {
            if(m_BatchIndex >= m_Handles.Count)
                return;

            var batchHandle = m_Handles[m_BatchIndex];
            var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();

            for (var i = 0; i < instanceDataBuffer.InstanceCount; i++)
            {
                var transformMatrix = instanceDataBuffer.GetTRS(i);
                
                var translation = new float3(transformMatrix.c3.x, transformMatrix.c3.y, transformMatrix.c3.z);
                var rotation = new quaternion(math.orthonormalize(new float3x3(transformMatrix)));
                var scale = new float3(math.length(transformMatrix.c0.xyz), math.length(transformMatrix.c1.xyz), math.length(transformMatrix.c2.xyz));
                
                Debug.Log($"[{i}] translation: {translation.ToString()}, rotation: {rotation.ToString()}, scale: {scale.ToString()}");
            }
        }

        // set random data
        private void SetRandomDataForBatchDataBuffer(in BatchInstanceDataBuffer instanceDataBuffer, int from, int count)
        {
            for (var i = from; i < from + count; i++)
            {
                // create random transform matrix for each instance
                var translation = transform.position + Random.insideUnitSphere * 100.0f;
                var rotation = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
                var scale = new float3(Random.Range(0.1f, 1.2f));

                var matrix = float4x4.TRS(translation, rotation, scale);
                
                instanceDataBuffer.SetTRS(i, matrix); // set the transform matrix to buffer
            }
        }

        private void OnDestroy()
        {
            // we must dispose the BRG container
            m_Container.Dispose();
        }
    }
}