namespace Samples.Create_BRG
{
    using UnityEngine;
    using BrgContainer.Runtime;
    
    [DisallowMultipleComponent]
    public sealed class CreateBRGSample : MonoBehaviour
    {
        private BatchRendererGroupContainer m_Container;

        private void Awake()
        {
            var boundsCenter = Vector3.zero;
            var boundsSize = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            // by these bounds BRG culling by default, so if we want to draw the all objects we need to create a bounds with float.MaxValue size
            var bounds = new Bounds(boundsCenter, boundsSize);
            
            // create a BRG container
            m_Container = new BatchRendererGroupContainer(bounds);
        }

        private void OnDestroy()
        {
            // we must dispose the BRG container
            m_Container.Dispose();
        }
    }
}