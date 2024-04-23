namespace Samples.Hello_World
{
    using BrgContainer.Runtime;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Random = UnityEngine.Random;

    public sealed class SampleBrgContainer : MonoBehaviour
    {
        private static readonly int m_ObjectToWorldPropertyId = Shader.PropertyToID("unity_ObjectToWorld");
        private static readonly int m_BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private Mesh m_Mesh;
        [SerializeField]
        private Material m_Material;

        [SerializeField]
        private int m_CubeCount = 1000;
        [SerializeField]
        private float m_Radius = 20;
        [SerializeField]
        private Weights m_Weights = Weights.Default();
        [SerializeField] 
        private float m_SeparationDistance = 10f;
        [SerializeField]
        private float m_MaxSpeed = 6f;
        [SerializeField]
        private float m_RotationSpeed = 4f;
        [SerializeField]
        private Transform m_Destination;

        private BatchRendererGroupContainer m_BrgContainer;
        private BatchHandle m_BatchHandle;

        private NativeReference<float3> m_CenterFlock;
        private NativeArray<float> m_NoiseOffsets;

        private JobHandle m_JobHandle;
        
        private void Start()
        {
            var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
            m_BrgContainer = new BatchRendererGroupContainer(bounds);
            
            var materialProperties = new NativeArray<MaterialProperty>(1, Allocator.Temp)
            {
                [0] = MaterialProperty.Create<Color>(m_BaseColorPropertyId)
            };
            var batchDescription = new BatchDescription(m_CubeCount, materialProperties, Allocator.Persistent);
            materialProperties.Dispose();

            var rendererDescription = new RendererDescription(ShadowCastingMode.On, true, false, 1, 0, MotionVectorGenerationMode.Camera);
            m_BatchHandle = m_BrgContainer.AddBatch(ref batchDescription, m_Mesh, 0, m_Material, rendererDescription);
            
            m_CenterFlock = new NativeReference<float3>(float3.zero, Allocator.Persistent);
            m_NoiseOffsets = new NativeArray<float>(m_CubeCount, Allocator.Persistent);

            var dataBuffer = m_BatchHandle.AsInstanceDataBuffer();
            dataBuffer.SetInstanceCount(m_CubeCount); // this number can be not a constant, but less than BatchDescription.MaxInstanceCount
            
            for (var i = 0; i < m_CubeCount; i++) // or use a IJobFor for initialization
            {
                var currentTransform = transform;
                var position = currentTransform.position + Random.insideUnitSphere * m_Radius;
                var rotation = Quaternion.Slerp(currentTransform.rotation, Random.rotation, 0.3f);
                m_NoiseOffsets[i] = Random.value * 10f;
                
                dataBuffer.SetTRS(i, position, rotation, Vector3.one);
                dataBuffer.SetColor(i, m_BaseColorPropertyId, new Color(0.2f, 0.2f, 0.8f));
            }
        }

        private void Update()
        {
            m_JobHandle.Complete();

            transform.position = m_CenterFlock.Value;
            m_BatchHandle.Upload();
            
            var dataBuffer = m_BatchHandle.AsInstanceDataBuffer();

            var averageCenterJob = new AverageCenterJob
            {
                InstanceDataBuffer = dataBuffer,
                Center = m_CenterFlock,
                Size = m_CubeCount,
                ObjectToWorldPropertyId = m_ObjectToWorldPropertyId
            };
            var averageCenterHandle = averageCenterJob.ScheduleByRef();

            var sampleJob = new SampleJob
            {
                Weights = m_Weights,
                Goal = m_Destination.position,
                NoiseOffsets = m_NoiseOffsets,
                Time = Time.time,
                DeltaTime = Time.deltaTime,
                MaxDistance = m_SeparationDistance,
                Speed = m_MaxSpeed,
                RotationSpeed = m_RotationSpeed,
                Size = m_CubeCount,
                InstanceDataBuffer = dataBuffer,
                ObjectToWorldPropertyId = m_ObjectToWorldPropertyId
            };
            m_JobHandle = sampleJob.ScheduleParallelByRef(m_CubeCount, 32, averageCenterHandle);
        }

        private void OnDestroy()
        {
            m_JobHandle.Complete();
            
            m_BatchHandle.Destroy();
            m_BrgContainer?.Dispose();

            m_CenterFlock.Dispose();
            m_NoiseOffsets.Dispose();
        }
    }
}