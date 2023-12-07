namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    public struct RendererDescription
    {
        /// <summary>
        /// Specifies how instances from the draw commands in this draw range cast shadows.
        /// </summary>
        public ShadowCastingMode ShadowCastingMode;
        /// <summary>
        /// Indicates whether instances from draw commands in this draw range should receive shadows.
        /// </summary>
        public bool ReceiveShadows;
        /// <summary>
        /// Indicates whether instances from the draw commands in this draw range render into cached shadow maps.
        /// </summary>
        public bool StaticShadowCaster;
        /// <summary>
        /// The rendering layer mask to use for draw commands in this draw range.
        /// </summary>
        public uint RenderingLayerMask;
        /// <summary>
        /// The layer to use for draw commands in this draw range.
        /// </summary>
        public byte Layer;
        /// <summary>
        /// Specifies how to generate motion vectors in this draw range.
        /// </summary>
        public MotionVectorGenerationMode MotionMode;
    }
}