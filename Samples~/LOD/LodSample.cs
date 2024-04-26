namespace Samples_.LOD
{
    using System.Collections.Generic;
    using BrgContainer.Runtime;
    using BrgContainer.Runtime.Lod;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using LODGroup = BrgContainer.Runtime.Lod.LODGroup;
    using Random = UnityEngine.Random;
    
    [DisallowMultipleComponent]
    public sealed class LodSample : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        
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

                // get an instance data buffer that contains all data per instance
                var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();
                SetRandomDataForBatchDataBuffer(instanceDataBuffer, objectEntry.Color, instanceDataBuffer.Capacity);
                
                batchHandle.Upload(); // upload to GPU side
            }
        }

        private BatchHandle CreateBatch(ObjectEntry entry)
        {
            // create a temp native array for material properties and initialize it
            // note that transform matrices (object to world and world to object) are already exist
            var materialProperties = new NativeArray<MaterialProperty>(1, Allocator.Temp)
            {
                [0] = MaterialProperty.Create<Color>(BaseColorPropertyId)
            };
            
            var batchDescription = new BatchDescription(entry.MaxCount, materialProperties, Allocator.Persistent);
            var rendererDescription = new RendererDescription(entry.ShadowCastingMode, entry.ReceiveShadows,
                entry.StaticShadowCaster, 1, 0, MotionVectorGenerationMode.Camera);

            // create LOD group for this batch
            var lodGroup = new LODGroup();
            lodGroup.LODs = new LODMeshData[math.min(entry.LODs.Length, 8)];
            lodGroup.FadeMode = entry.FadeMode;

            // only eight levels supports
            for (var lod = 0; lod < math.min(entry.LODs.Length, 8); lod++)
            {
                var lodDescription = entry.LODs[lod];
                lodGroup.LODs[lod] = new LODMeshData
                {
                    Mesh = lodDescription.Mesh,
                    Material = lodDescription.Material,
                    SubMeshIndex = 0,
                    ScreenRelativeTransitionHeight = lodDescription.ScreenRelativeTransitionHeight,
                    FadeTransitionWidth = lodDescription.FadeTransitionWidth
                };
            }

            return m_Container.AddBatch(ref batchDescription, ref lodGroup, float3.zero, rendererDescription);
        }
        
        [ContextMenu("Set Random Data")]
        private void SetRandomData()
        {
            foreach (var batchHandle in m_Handles)
            {
                var instanceDataBuffer = batchHandle.AsInstanceDataBuffer();
                var instanceCount = Random.Range(0, instanceDataBuffer.Capacity);
                var randomColor = Random.ColorHSV();
                    
                SetRandomDataForBatchDataBuffer(instanceDataBuffer, randomColor, instanceCount);
                
                batchHandle.Upload(); // upload to GPU side
            }
        }

        private void SetRandomDataForBatchDataBuffer(in BatchInstanceDataBuffer instanceDataBuffer, Color color, int instanceCount)
        {
            var maxInstanceCount = instanceDataBuffer.Capacity;
            instanceDataBuffer.SetInstanceCount(instanceCount); // set current instance count
            
            for (var i = 0; i < maxInstanceCount; i++)
            {
                // create random transform matrix for each instance
                var translation = transform.position + Random.insideUnitSphere * 100.0f;
                var rotation = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
                var scale = new float3(Random.Range(0.1f, 1.2f));

                var matrix = float4x4.TRS(translation, rotation, scale);
                
                instanceDataBuffer.SetTRS(i, matrix); // set the transform matrix to buffer
                instanceDataBuffer.SetColor(i, BaseColorPropertyId, color);
            }
        }

        private void OnDestroy()
        {
            // we must dispose the BRG container
            m_Container.Dispose();
        }
    }
}